using FluentAssertions;
using InventoryAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace InventoryAPI.IntegrationTests;

public sealed class MigrationRehearsalTests
{
    private const string InitialMigration = "20260715222734_InitialCreate";
    private const string LedgerMigration = "20260726090000_EnforceLedgerInvariants";

    [Fact]
    public async Task InitialToLatest_ReconcilesLegacyDataAndRevokesBearerTokens()
    {
        await using var database = await MigrationDatabase.CreateAsync();
        await MigrateAsync(database, InitialMigration);

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var movementId = Guid.NewGuid();
        await SeedUserAndProductAsync(database, userId, productId, currentStock: 12, "legacy-bearer-token");
        await database.ExecuteAsync(
            $"""
            INSERT INTO "StockMovements" (
                "Id", "ProductId", "Type", "Quantity", "SourceLocation",
                "DestinationLocation", "Reason", "Reference", "WorkOrderId",
                "PerformedById", "Timestamp", "UnitCostAtTransaction")
            VALUES (
                '{movementId}', '{productId}', 1, 10, 'Supplier',
                'Main', 'Legacy receipt', 'LEGACY-001', NULL,
                '{userId}', TIMESTAMPTZ '2026-01-02 00:00:00+00', 2.50);
            """);

        await MigrateAsync(database, LedgerMigration);

        var tokenRevoked = (bool)(await database.ScalarAsync(
            $"""SELECT "RefreshTokenHash" IS NULL FROM "Users" WHERE "Id" = '{userId}';"""))!;
        var movements = await database.ReadMovementSnapshotsAsync(productId);

        tokenRevoked.Should().BeTrue();
        movements.Should().HaveCount(2);
        movements.Should().SatisfyRespectively(
            reconciliation =>
            {
                reconciliation.Type.Should().Be(3);
                reconciliation.Quantity.Should().Be(2);
                reconciliation.BalanceBefore.Should().Be(0);
                reconciliation.BalanceAfter.Should().Be(2);
                reconciliation.Reference.Should().Be("MIGRATION:20260726090000");
                reconciliation.OperationId.Should().NotBe(Guid.Empty);
            },
            receipt =>
            {
                receipt.Type.Should().Be(1);
                receipt.Quantity.Should().Be(10);
                receipt.BalanceBefore.Should().Be(2);
                receipt.BalanceAfter.Should().Be(12);
                receipt.Reference.Should().Be("LEGACY-001");
                receipt.OperationId.Should().NotBe(Guid.Empty);
            });

        var mutation = () => database.ExecuteAsync(
            $"""UPDATE "StockMovements" SET "Reason" = 'tampered' WHERE "Id" = '{movementId}';""");
        var mutationFailure = await mutation.Should().ThrowAsync<PostgresException>();
        mutationFailure.Which.SqlState.Should().Be("55000");

        var drift = () => database.CommitCachedBalanceDriftAsync(productId, 13);
        var driftFailure = await drift.Should().ThrowAsync<PostgresException>();
        driftFailure.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task LatestToInitialToLatest_PreservesRowsAndReinstallsLedgerGuards()
    {
        await using var database = await MigrationDatabase.CreateAsync();
        await MigrateAsync(database, InitialMigration);

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var movementId = Guid.NewGuid();
        await SeedUserAndProductAsync(database, userId, productId, currentStock: 10, null);
        await database.ExecuteAsync(
            $"""
            INSERT INTO "StockMovements" (
                "Id", "ProductId", "Type", "Quantity", "SourceLocation",
                "DestinationLocation", "Reason", "Reference", "WorkOrderId",
                "PerformedById", "Timestamp", "UnitCostAtTransaction")
            VALUES (
                '{movementId}', '{productId}', 1, 10, 'Supplier',
                'Main', 'Legacy receipt', 'LEGACY-002', NULL,
                '{userId}', TIMESTAMPTZ '2026-02-01 00:00:00+00', 3.00);
            """);

        await MigrateAsync(database, LedgerMigration);
        await MigrateAsync(database, InitialMigration);

        (await database.ColumnExistsAsync("Products", "CostingMethod")).Should().BeTrue();
        (await database.ColumnExistsAsync("Users", "RefreshToken")).Should().BeTrue();
        (await database.ColumnExistsAsync("StockMovements", "OperationId")).Should().BeFalse();
        ((long)(await database.ScalarAsync(
            $"""SELECT COUNT(*) FROM "StockMovements" WHERE "ProductId" = '{productId}';"""))!)
            .Should().Be(1);

        await MigrateAsync(database, LedgerMigration);

        (await database.ColumnExistsAsync("Products", "CostingMethod")).Should().BeFalse();
        (await database.ColumnExistsAsync("Users", "RefreshTokenHash")).Should().BeTrue();
        (await database.ColumnExistsAsync("StockMovements", "OperationId")).Should().BeTrue();
        ((long)(await database.ScalarAsync(
            $"""SELECT COUNT(*) FROM "StockMovements" WHERE "ProductId" = '{productId}';"""))!)
            .Should().Be(1);

        var mutation = () => database.ExecuteAsync(
            $"""DELETE FROM "StockMovements" WHERE "Id" = '{movementId}';""");
        var failure = await mutation.Should().ThrowAsync<PostgresException>();
        failure.Which.SqlState.Should().Be("55000");
    }

    [Theory]
    [InlineData("negative-history")]
    [InlineData("duplicate-work-order-product")]
    public async Task UnsafeLegacyData_FailsWithoutPartiallyApplyingMigration(string scenario)
    {
        await using var database = await MigrationDatabase.CreateAsync();
        await MigrateAsync(database, InitialMigration);

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedUserAndProductAsync(database, userId, productId, currentStock: 0, "still-live");

        if (scenario == "negative-history")
        {
            await SeedNegativeHistoryAsync(database, userId, productId);
        }
        else
        {
            await SeedDuplicateWorkOrderProductAsync(database, userId, productId);
        }

        var migration = () => MigrateAsync(database, LedgerMigration);
        var failure = await migration.Should().ThrowAsync<Exception>();
        failure.Which.GetBaseException().Should().BeOfType<PostgresException>();

        (await database.ColumnExistsAsync("Products", "CostingMethod")).Should().BeTrue();
        (await database.ColumnExistsAsync("Users", "RefreshToken")).Should().BeTrue();
        (await database.ColumnExistsAsync("Users", "RefreshTokenHash")).Should().BeFalse();
        (await database.ColumnExistsAsync("StockMovements", "OperationId")).Should().BeFalse();
        ((long)(await database.ScalarAsync(
            $"""
            SELECT COUNT(*)
            FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '{LedgerMigration}';
            """))!).Should().Be(0);
        ((string)(await database.ScalarAsync(
            $"""SELECT "RefreshToken" FROM "Users" WHERE "Id" = '{userId}';"""))!)
            .Should().Be("still-live");
    }

    private static async Task MigrateAsync(MigrationDatabase database, string target)
    {
        await using var context = database.CreateContext();
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(target);
    }

    private static Task SeedUserAndProductAsync(
        MigrationDatabase database,
        Guid userId,
        Guid productId,
        int currentStock,
        string? refreshToken) =>
        database.ExecuteAsync(
            $"""
            INSERT INTO "Users" (
                "Id", "Email", "PasswordHash", "FirstName", "LastName", "Role",
                "IsActive", "RefreshToken", "RefreshTokenExpiryTime", "CreatedAt",
                "CreatedBy", "IsDeleted")
            VALUES (
                '{userId}', 'legacy@example.com', 'legacy-hash', 'Legacy', 'Operator', 3,
                TRUE, {(refreshToken == null ? "NULL" : $"'{refreshToken}'")},
                {(refreshToken == null ? "NULL" : "TIMESTAMPTZ '2026-12-01 00:00:00+00'")},
                TIMESTAMPTZ '2026-01-01 00:00:00+00', 'legacy-import', FALSE);

            INSERT INTO "Products" (
                "Id", "SKU", "Name", "Description", "Category", "CurrentStock",
                "ReorderPoint", "ReorderQuantity", "UnitOfMeasure", "UnitCost",
                "Location", "CostingMethod", "CreatedAt", "CreatedBy", "IsDeleted")
            VALUES (
                '{productId}', 'LEGACY-001', 'Legacy Widget', 'Migration rehearsal',
                'Components', {currentStock}, 2, 5, 'Each', 2.50,
                'Main', 0, TIMESTAMPTZ '2026-01-01 00:00:00+00',
                'legacy-import', FALSE);
            """);

    private static Task SeedNegativeHistoryAsync(
        MigrationDatabase database,
        Guid userId,
        Guid productId) =>
        database.ExecuteAsync(
            $"""
            INSERT INTO "StockMovements" (
                "Id", "ProductId", "Type", "Quantity", "SourceLocation",
                "DestinationLocation", "Reason", "PerformedById", "Timestamp",
                "UnitCostAtTransaction")
            VALUES
                ('{Guid.NewGuid()}', '{productId}', 2, 1, 'Main', 'Consumed',
                 'Legacy issue before receipt', '{userId}',
                 TIMESTAMPTZ '2026-03-01 00:00:00+00', 2.50),
                ('{Guid.NewGuid()}', '{productId}', 1, 1, 'Supplier', 'Main',
                 'Late legacy receipt', '{userId}',
                 TIMESTAMPTZ '2026-03-02 00:00:00+00', 2.50);
            """);

    private static Task SeedDuplicateWorkOrderProductAsync(
        MigrationDatabase database,
        Guid userId,
        Guid productId)
    {
        var workOrderId = Guid.NewGuid();
        return database.ExecuteAsync(
            $"""
            INSERT INTO "WorkOrders" (
                "Id", "OrderNumber", "Priority", "Status", "Title", "Description",
                "RequestedById", "CreatedAt", "CreatedBy", "IsDeleted")
            VALUES (
                '{workOrderId}', 'WO-LEGACY-001', 1, 1, 'Legacy duplicate',
                'Migration rehearsal', '{userId}',
                TIMESTAMPTZ '2026-03-01 00:00:00+00', 'legacy-import', FALSE);

            INSERT INTO "WorkOrderItems" (
                "Id", "WorkOrderId", "ProductId", "QuantityRequested", "QuantityIssued")
            VALUES
                ('{Guid.NewGuid()}', '{workOrderId}', '{productId}', 1, 0),
                ('{Guid.NewGuid()}', '{workOrderId}', '{productId}', 2, 0);
            """);
    }

    private sealed record MovementSnapshot(
        int Type,
        int Quantity,
        int BalanceBefore,
        int BalanceAfter,
        string? Reference,
        Guid OperationId);

    private sealed class MigrationDatabase : IAsyncDisposable
    {
        private readonly string _adminConnection;
        private bool _dropped;

        private MigrationDatabase(string adminConnection, string connectionString, string databaseName)
        {
            _adminConnection = adminConnection;
            ConnectionString = connectionString;
            DatabaseName = databaseName;
        }

        public string ConnectionString { get; }
        public string DatabaseName { get; }

        public static async Task<MigrationDatabase> CreateAsync()
        {
            var suppliedConnection = Environment.GetEnvironmentVariable("STOCKVERITY_TEST_POSTGRES");
            if (string.IsNullOrWhiteSpace(suppliedConnection))
            {
                throw new InvalidOperationException(
                    "STOCKVERITY_TEST_POSTGRES is required for PostgreSQL migration rehearsals.");
            }

            var databaseName = $"stockverity_migrations_{Guid.NewGuid():N}";
            var adminBuilder = new NpgsqlConnectionStringBuilder(suppliedConnection)
            {
                Database = "postgres",
                Pooling = false
            };
            var databaseBuilder = new NpgsqlConnectionStringBuilder(suppliedConnection)
            {
                Database = databaseName,
                Pooling = false
            };

            await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"CREATE DATABASE {QuoteIdentifier(databaseName)}",
                connection);
            await command.ExecuteNonQueryAsync();

            return new MigrationDatabase(
                adminBuilder.ConnectionString,
                databaseBuilder.ConnectionString,
                databaseName);
        }

        public ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new ApplicationDbContext(options);
        }

        public async Task ExecuteAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<object?> ScalarAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            return await command.ExecuteScalarAsync();
        }

        public async Task<bool> ColumnExistsAsync(string table, string column)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = @table
                      AND column_name = @column);
                """,
                connection);
            command.Parameters.AddWithValue("table", table);
            command.Parameters.AddWithValue("column", column);
            return (bool)(await command.ExecuteScalarAsync())!;
        }

        public async Task<IReadOnlyList<MovementSnapshot>> ReadMovementSnapshotsAsync(Guid productId)
        {
            var movements = new List<MovementSnapshot>();
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                SELECT "Type", "Quantity", "BalanceBefore", "BalanceAfter", "Reference", "OperationId"
                FROM "StockMovements"
                WHERE "ProductId" = @productId
                ORDER BY "Timestamp", "Id";
                """,
                connection);
            command.Parameters.AddWithValue("productId", productId);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                movements.Add(new MovementSnapshot(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.GetGuid(5)));
            }

            return movements;
        }

        public async Task CommitCachedBalanceDriftAsync(Guid productId, int currentStock)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = new NpgsqlCommand(
                """UPDATE "Products" SET "CurrentStock" = @currentStock WHERE "Id" = @productId;""",
                connection,
                transaction);
            command.Parameters.AddWithValue("currentStock", currentStock);
            command.Parameters.AddWithValue("productId", productId);
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_dropped)
            {
                return;
            }

            _dropped = true;
            NpgsqlConnection.ClearAllPools();

            await using var connection = new NpgsqlConnection(_adminConnection);
            await connection.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             """
                             SELECT pg_terminate_backend(pid)
                             FROM pg_stat_activity
                             WHERE datname = @database AND pid <> pg_backend_pid();
                             """,
                             connection))
            {
                terminate.Parameters.AddWithValue("database", DatabaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS {QuoteIdentifier(DatabaseName)}",
                connection);
            await drop.ExecuteNonQueryAsync();
        }

        private static string QuoteIdentifier(string value) =>
            '"' + value.Replace("\"", "\"\"") + '"';
    }
}

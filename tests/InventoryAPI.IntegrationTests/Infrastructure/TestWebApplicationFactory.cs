using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace InventoryAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real HTTP pipeline against a uniquely named PostgreSQL database.
/// The suite intentionally has no in-memory fallback because provider-specific
/// constraints, xmin concurrency, migrations, and append-only triggers are part
/// of the behavior under test.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _adminConnection;
    private readonly string _testConnection;
    private readonly string _dataProtectionPath;
    private bool _databaseDropped;

    public TestWebApplicationFactory()
    {
        var suppliedConnection = Environment.GetEnvironmentVariable("STOCKVERITY_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(suppliedConnection))
        {
            throw new InvalidOperationException(
                "STOCKVERITY_TEST_POSTGRES is required. Integration tests must run against PostgreSQL; " +
                "there is deliberately no in-memory fallback.");
        }

        DatabaseName = $"stockverity_tests_{Guid.NewGuid():N}";

        var adminBuilder = new NpgsqlConnectionStringBuilder(suppliedConnection)
        {
            Database = "postgres",
            Pooling = false
        };
        var testBuilder = new NpgsqlConnectionStringBuilder(suppliedConnection)
        {
            Database = DatabaseName,
            Pooling = false
        };

        _adminConnection = adminBuilder.ConnectionString;
        _testConnection = testBuilder.ConnectionString;
        _dataProtectionPath = Path.Combine(Path.GetTempPath(), $"stockverity-dp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataProtectionPath);

        CreateDatabase();
    }

    public string DatabaseName { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _testConnection);
        builder.UseSetting("JwtSettings:SecretKey", new string('x', 64));
        builder.UseSetting("JwtSettings:Issuer", "StockVerity");
        builder.UseSetting("JwtSettings:Audience", "StockVerityUsers");
        builder.UseSetting("JwtSettings:ExpiryMinutes", "60");
        builder.UseSetting("JwtSettings:RefreshTokenExpiryDays", "7");
        builder.UseSetting("OpenApi:Enabled", "false");
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("DemoData:Enabled", "true");
        builder.UseSetting("HttpsRedirection:Enabled", "false");
        builder.UseSetting("DataProtection:KeysPath", _dataProtectionPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _databaseDropped)
        {
            return;
        }

        _databaseDropped = true;
        DropDatabaseBestEffort();

        try
        {
            Directory.Delete(_dataProtectionPath, recursive: true);
        }
        catch (IOException)
        {
            // Test cleanup is best effort; the temporary directory contains no
            // source or credentials and will be removed by the runner lifecycle.
        }
        catch (UnauthorizedAccessException)
        {
            // Same best-effort cleanup policy as above.
        }
    }

    private void CreateDatabase()
    {
        using var connection = new NpgsqlConnection(_adminConnection);
        connection.Open();

        var quotedName = QuoteIdentifier(DatabaseName);
        using var command = new NpgsqlCommand($"CREATE DATABASE {quotedName}", connection);
        command.ExecuteNonQuery();
    }

    private void DropDatabaseBestEffort()
    {
        try
        {
            NpgsqlConnection.ClearAllPools();

            using var connection = new NpgsqlConnection(_adminConnection);
            connection.Open();

            using (var terminate = new NpgsqlCommand(
                       "SELECT pg_terminate_backend(pid) " +
                       "FROM pg_stat_activity " +
                       "WHERE datname = @database AND pid <> pg_backend_pid()",
                       connection))
            {
                terminate.Parameters.AddWithValue("database", DatabaseName);
                terminate.ExecuteNonQuery();
            }

            using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS {QuoteIdentifier(DatabaseName)}",
                connection);
            drop.ExecuteNonQuery();
        }
        catch (NpgsqlException)
        {
            // CI uses an ephemeral PostgreSQL service. A cleanup failure must
            // not conceal the test result that produced the database.
        }
    }

    private static string QuoteIdentifier(string value) =>
        '"' + value.Replace("\"", "\"\"") + '"';
}

using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Models;
using InventoryAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryAPI.Infrastructure.Services;

/// <summary>
/// Handles database initialization, migrations, and seeding for all scenarios
/// </summary>
public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly bool _applyMigrations;
    private readonly bool _seedDemoData;

    public DatabaseInitializationService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
        _applyMigrations = configuration.GetValue<bool>("Database:ApplyMigrations");
        _seedDemoData = configuration.GetValue<bool>("DemoData:Enabled");
    }

    /// <summary>
    /// Initializes and verifies the database according to explicit configuration flags
    /// </summary>
    public async Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var result = new DatabaseInitializationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting database initialization...");

            // Step 1: Check connection with retry
            result.CanConnect = await CheckConnectionWithRetryAsync(cancellationToken);
            if (!result.CanConnect)
            {
                result.Success = false;
                result.ErrorMessage = "Unable to connect to database after multiple retry attempts";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            _logger.LogInformation("Database connection established successfully");

            // Step 2: Check for pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            result.PendingMigrationsCount = pendingMigrations.Count();
            result.PendingMigrations = pendingMigrations.ToList();

            if (result.PendingMigrationsCount > 0)
            {
                _logger.LogInformation(
                    "Found {Count} pending migration(s): {Migrations}",
                    result.PendingMigrationsCount,
                    string.Join(", ", result.PendingMigrations));

                if (!_applyMigrations)
                {
                    result.Success = false;
                    result.ErrorMessage =
                        "Pending migrations exist and Database:ApplyMigrations is disabled.";
                    return result;
                }

                try
                {
                    await _context.Database.MigrateAsync(cancellationToken);
                    result.MigrationsApplied = true;
                    _logger.LogInformation("All migrations applied successfully");
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Migration failed: {ex.Message}";
                    _logger.LogError(ex, "Error applying migrations");
                    return result;
                }
            }
            else
            {
                _logger.LogInformation("Database is up to date. No pending migrations.");
            }

            // Step 4: Get current migration version
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            result.CurrentMigration = appliedMigrations.LastOrDefault() ?? "None";
            result.TotalMigrationsApplied = appliedMigrations.Count();

            _logger.LogInformation($"Current migration version: {result.CurrentMigration}");

            if (_seedDemoData)
            {
                try
                {
                    await DatabaseSeeder.SeedAsync(_context, _passwordService);
                    result.DataSeeded = true;
                    _logger.LogInformation("Explicitly enabled demo data was seeded successfully");
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Demo data seeding failed: {ex.Message}";
                    _logger.LogError(ex, "Demo data seeding failed");
                    return result;
                }
            }
            else
            {
                _logger.LogInformation("Demo data seeding is disabled");
            }

            result.Success = true;
            stopwatch.Stop();
            result.InitializationTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation($"Database initialization completed successfully in {result.InitializationTimeMs}ms");

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.InitializationTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Database initialization failed");
            return result;
        }
    }

    /// <summary>
    /// Verifies that the database is reachable and has no pending migrations
    /// </summary>
    public async Task<DatabaseInitializationResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var result = new DatabaseInitializationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Verifying database state...");

            // Check connection
            result.CanConnect = await CheckConnectionWithRetryAsync(cancellationToken);
            if (!result.CanConnect)
            {
                result.Success = false;
                result.ErrorMessage = "Cannot connect to database";
                _logger.LogCritical("Unable to connect to database");
                return result;
            }

            // Check for pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            result.PendingMigrationsCount = pendingMigrations.Count();
            result.PendingMigrations = pendingMigrations.ToList();

            if (result.PendingMigrationsCount > 0)
            {
                result.Success = false;
                result.ErrorMessage = $"Database has {result.PendingMigrationsCount} pending migration(s). Migrations must be applied before starting in Production.";
                _logger.LogCritical("Pending migrations detected: {Migrations}",
                    string.Join(", ", result.PendingMigrations));
                return result;
            }

            // Get current state
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            result.CurrentMigration = appliedMigrations.LastOrDefault() ?? "None";
            result.TotalMigrationsApplied = appliedMigrations.Count();

            result.Success = true;
            stopwatch.Stop();
            result.InitializationTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Database verification successful. Current migration: {Migration}", result.CurrentMigration);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.InitializationTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogCritical(ex, "Database verification error");
            return result;
        }
    }

    /// <summary>
    /// Gets current database status for health checks
    /// </summary>
    public async Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new DatabaseStatus();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            status.CanConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (status.CanConnect)
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);

                status.PendingMigrationsCount = pendingMigrations.Count();
                status.CurrentMigration = appliedMigrations.LastOrDefault() ?? "None";
                status.TotalMigrationsApplied = appliedMigrations.Count();
                status.IsHealthy = status.PendingMigrationsCount == 0;
            }
            else
            {
                status.IsHealthy = false;
                status.ErrorMessage = "Cannot connect to database";
            }

            stopwatch.Stop();
            status.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.CanConnect = false;
            status.ErrorMessage = ex.Message;
            stopwatch.Stop();
            status.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return status;
    }

    /// <summary>
    /// Checks database connection with exponential backoff retry
    /// </summary>
    private async Task<bool> CheckConnectionWithRetryAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        var delays = new[] { 1000, 2000, 4000, 8000, 16000 }; // Exponential backoff in ms

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                if (canConnect)
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Database connection successful on attempt {Attempt}", attempt);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection attempt {Attempt}/{MaxRetries} failed",
                    attempt, maxRetries);
            }

            if (attempt < maxRetries)
            {
                var delay = delays[attempt - 1];
                _logger.LogInformation("Retrying database connection in {Delay}ms...", delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError("Failed to connect to database after {MaxRetries} attempts", maxRetries);
        return false;
    }
}

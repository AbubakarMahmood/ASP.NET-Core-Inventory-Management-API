using InventoryAPI.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InventoryAPI.Api.HealthChecks;

/// <summary>
/// Comprehensive database health check with detailed status information
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseInitializationService _databaseService;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        IDatabaseInitializationService databaseService,
        ILogger<DatabaseHealthCheck> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _databaseService.GetStatusAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["canConnect"] = status.CanConnect,
                ["currentMigration"] = status.CurrentMigration,
                ["totalMigrations"] = status.TotalMigrationsApplied,
                ["pendingMigrations"] = status.PendingMigrationsCount,
                ["responseTimeMs"] = status.ResponseTimeMs
            };

            if (!string.IsNullOrEmpty(status.ErrorMessage))
            {
                data["error"] = status.ErrorMessage;
            }

            if (!status.CanConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    data: data);
            }

            if (status.PendingMigrationsCount > 0)
            {
                return HealthCheckResult.Degraded(
                    $"Database has {status.PendingMigrationsCount} pending migration(s)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Database is healthy and up to date",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            return HealthCheckResult.Unhealthy(
                "Health check encountered an error",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}

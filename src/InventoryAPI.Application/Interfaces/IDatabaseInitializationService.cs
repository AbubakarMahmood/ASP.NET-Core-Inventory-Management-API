using InventoryAPI.Infrastructure.Services;

namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Service for database initialization, migrations, and health monitoring
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Initialize database with migrations and seeding (Development)
    /// </summary>
    Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify database is ready without applying migrations (Production)
    /// </summary>
    Task<DatabaseInitializationResult> VerifyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current database status for health checks
    /// </summary>
    Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

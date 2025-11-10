namespace InventoryAPI.Application.Models;

/// <summary>
/// Current database status
/// </summary>
public class DatabaseStatus
{
    public bool IsHealthy { get; set; }
    public bool CanConnect { get; set; }
    public int PendingMigrationsCount { get; set; }
    public string CurrentMigration { get; set; } = string.Empty;
    public int TotalMigrationsApplied { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

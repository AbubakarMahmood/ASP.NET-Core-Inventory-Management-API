namespace InventoryAPI.Application.Models;

/// <summary>
/// Result of database initialization
/// </summary>
public class DatabaseInitializationResult
{
    public bool Success { get; set; }
    public bool CanConnect { get; set; }
    public int PendingMigrationsCount { get; set; }
    public List<string> PendingMigrations { get; set; } = new();
    public bool MigrationsApplied { get; set; }
    public string CurrentMigration { get; set; } = string.Empty;
    public int TotalMigrationsApplied { get; set; }
    public bool DataSeeded { get; set; }
    public long InitializationTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

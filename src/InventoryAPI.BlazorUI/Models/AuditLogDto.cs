namespace InventoryAPI.BlazorUI.Models;

/// <summary>
/// Audit log entry DTO
/// </summary>
public class AuditLogDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityIdentifier { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
}

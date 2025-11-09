namespace InventoryAPI.Application.DTOs;

/// <summary>
/// Audit log entry for tracking changes
/// </summary>
public class AuditLogDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityIdentifier { get; set; } = string.Empty; // SKU, OrderNumber, Email, etc.
    public string Action { get; set; } = string.Empty; // Created, Modified, Deleted
    public DateTime Timestamp { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
}

using InventoryAPI.Domain.Common;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Saved filter preset for various entity types
/// </summary>
public class FilterPreset : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // "Product", "WorkOrder", "AuditLog"
    public string FilterData { get; set; } = string.Empty; // JSON string with filter criteria
    public bool IsDefault { get; set; } = false;
    public bool IsShared { get; set; } = false;

    // Navigation properties
    public User User { get; set; } = null!;
}

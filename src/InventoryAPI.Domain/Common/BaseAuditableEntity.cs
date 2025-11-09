namespace InventoryAPI.Domain.Common;

/// <summary>
/// Base auditable entity with audit trail fields
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

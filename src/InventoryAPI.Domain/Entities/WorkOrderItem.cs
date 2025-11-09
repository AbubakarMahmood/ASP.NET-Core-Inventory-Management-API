using InventoryAPI.Domain.Common;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Junction entity for WorkOrder and Product with quantity
/// </summary>
public class WorkOrderItem : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityRequested { get; set; }
    public int QuantityIssued { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public WorkOrder WorkOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

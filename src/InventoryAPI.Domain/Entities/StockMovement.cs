using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Enums;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Stock movement entity for tracking inventory changes
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid PerformedById { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public decimal UnitCostAtTransaction { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public User PerformedBy { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
}

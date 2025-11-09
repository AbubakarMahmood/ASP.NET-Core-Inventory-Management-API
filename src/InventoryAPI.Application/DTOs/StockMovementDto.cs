using InventoryAPI.Domain.Enums;

namespace InventoryAPI.Application.DTOs;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public Guid PerformedById { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal UnitCostAtTransaction { get; set; }
}

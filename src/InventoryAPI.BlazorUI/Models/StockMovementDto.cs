namespace InventoryAPI.BlazorUI.Models;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid PerformedById { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public int CurrentStock { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum StockMovementType
{
    Receipt = 0,
    Issue = 1,
    Adjustment = 2,
    Transfer = 3,
    Return = 4
}

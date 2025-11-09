namespace InventoryAPI.BlazorUI.Models;

public class RecordStockMovementRequest
{
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? WorkOrderId { get; set; }
}

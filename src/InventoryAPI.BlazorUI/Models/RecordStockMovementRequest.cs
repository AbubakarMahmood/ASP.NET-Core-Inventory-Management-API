namespace InventoryAPI.BlazorUI.Models;

public class RecordStockMovementRequest
{
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; } = StockMovementType.Receipt;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

namespace InventoryAPI.Application.DTOs;

/// <summary>
/// Aggregate statistics over stock movements in a date range
/// </summary>
public class StockMovementStatisticsDto
{
    public int TotalMovements { get; set; }
    public int ReceiptCount { get; set; }
    public int IssueCount { get; set; }
    public int AdjustmentCount { get; set; }
    public int TransferCount { get; set; }
    public int ReturnCount { get; set; }
    public int TotalQuantityIn { get; set; }
    public int TotalQuantityOut { get; set; }
    public int UniqueProducts { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

namespace InventoryAPI.Application.DTOs;

/// <summary>
/// Aggregate ledger statistics over a date range.
/// </summary>
public class StockMovementStatisticsDto
{
    public int TotalMovements { get; set; }
    public int OpeningBalanceCount { get; set; }
    public int ReceiptCount { get; set; }
    public int IssueCount { get; set; }
    public int AdjustmentCount { get; set; }
    public int LegacyTransferCount { get; set; }
    public int ReturnCount { get; set; }
    public long TotalQuantityIn { get; set; }
    public long TotalQuantityOut { get; set; }
    public long NetQuantityChange { get; set; }
    public int UniqueProducts { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

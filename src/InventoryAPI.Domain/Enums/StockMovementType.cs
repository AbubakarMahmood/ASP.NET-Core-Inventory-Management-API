namespace InventoryAPI.Domain.Enums;

/// <summary>
/// Types of stock movements
/// </summary>
public enum StockMovementType
{
    Receipt = 1,
    Issue = 2,
    Adjustment = 3,
    Transfer = 4,
    Return = 5
}

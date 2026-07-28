namespace InventoryAPI.Domain.Enums;

/// <summary>
/// Persisted stock movement types. Numeric values are stable because they are
/// stored in PostgreSQL. Transfer remains reserved for historical compatibility
/// but is rejected until the multi-location RFC is implemented.
/// </summary>
public enum StockMovementType
{
    Receipt = 1,
    Issue = 2,
    Adjustment = 3,
    Transfer = 4,
    Return = 5,
    OpeningBalance = 6
}

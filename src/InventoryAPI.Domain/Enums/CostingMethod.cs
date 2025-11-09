namespace InventoryAPI.Domain.Enums;

/// <summary>
/// Inventory costing methods
/// </summary>
public enum CostingMethod
{
    FIFO = 1,      // First In, First Out
    LIFO = 2,      // Last In, First Out
    Average = 3    // Weighted Average
}

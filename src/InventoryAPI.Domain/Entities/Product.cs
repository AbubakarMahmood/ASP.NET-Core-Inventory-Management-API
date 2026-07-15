using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Product/Item entity for inventory management
/// </summary>
public class Product : BaseAuditableEntity
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
    public CostingMethod CostingMethod { get; set; } = CostingMethod.Average;

    // Navigation properties
    public ICollection<WorkOrderItem> WorkOrderItems { get; set; } = new List<WorkOrderItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    // Domain methods
    public bool IsLowStock() => CurrentStock <= ReorderPoint;

    public void AdjustStock(int quantity)
    {
        var newStock = CurrentStock + quantity;
        if (newStock < 0)
            throw new InsufficientStockException(Id, CurrentStock, -quantity);

        CurrentStock = newStock;
    }
}

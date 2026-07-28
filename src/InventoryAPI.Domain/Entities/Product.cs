using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Catalog item with a cached on-hand balance. The balance may only be changed
/// through <see cref="ApplyStockDelta"/> so application handlers can persist an
/// immutable <see cref="StockMovement"/> in the same database commit.
/// </summary>
public class Product : BaseAuditableEntity
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; private set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;

    public ICollection<WorkOrderItem> WorkOrderItems { get; set; } = new List<WorkOrderItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public bool IsLowStock() => CurrentStock <= ReorderPoint;

    /// <summary>
    /// Applies a signed inventory delta to the cached balance. Positive values
    /// add stock; negative values remove stock. The caller is responsible for
    /// committing the matching immutable ledger entry atomically.
    /// </summary>
    public void ApplyStockDelta(int quantityDelta)
    {
        int newStock;
        try
        {
            newStock = checked(CurrentStock + quantityDelta);
        }
        catch (OverflowException ex)
        {
            throw new BusinessRuleViolationException(
                $"Applying stock delta {quantityDelta} would overflow the balance for product {Id}.", ex);
        }

        if (newStock < 0)
        {
            var requested = quantityDelta == int.MinValue ? int.MaxValue : Math.Abs(quantityDelta);
            throw new InsufficientStockException(Id, CurrentStock, requested);
        }

        CurrentStock = newStock;
    }
}

using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// A requested product quantity and the quantity already issued against a work
/// order.
/// </summary>
public class WorkOrderItem : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityRequested { get; set; }
    public int QuantityIssued { get; private set; }
    public string? Notes { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public int RemainingQuantity => QuantityRequested - QuantityIssued;
    public bool IsFullyIssued => QuantityIssued == QuantityRequested;

    public void Issue(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleViolationException("Issue quantity must be greater than zero.");
        }

        if (quantity > RemainingQuantity)
        {
            throw new BusinessRuleViolationException(
                $"Cannot issue {quantity} units. Remaining requested quantity is {RemainingQuantity}.");
        }

        QuantityIssued = checked(QuantityIssued + quantity);
    }
}

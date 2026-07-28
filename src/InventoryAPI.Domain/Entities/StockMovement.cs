using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Append-only evidence of one inventory balance change. New entries are
/// created through <see cref="Post"/>, which applies the product balance delta
/// and records before/after snapshots as one in-memory operation.
/// </summary>
public class StockMovement : BaseEntity
{
    public const string ExternalLocation = "EXTERNAL";
    public const string OpeningBalanceSource = "OPENING-BALANCE";

    private StockMovement()
    {
    }

    /// <summary>
    /// Caller-supplied idempotency key. A work-order issue batch deliberately
    /// shares one operation id across its per-product movement rows.
    /// </summary>
    public Guid OperationId { get; private set; }

    public Guid ProductId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public int BalanceBefore { get; private set; }
    public int BalanceAfter { get; private set; }
    public string SourceLocation { get; private set; } = string.Empty;
    public string DestinationLocation { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public Guid PerformedById { get; private set; }
    public DateTime Timestamp { get; private set; }
    public decimal UnitCostAtTransaction { get; private set; }

    public Product Product { get; private set; } = null!;
    public User PerformedBy { get; private set; } = null!;
    public WorkOrder? WorkOrder { get; private set; }

    /// <summary>
    /// Converts persisted movement semantics into the signed change applied to
    /// the cached product balance.
    /// </summary>
    public static int CalculateQuantityDelta(StockMovementType type, int quantity)
    {
        return type switch
        {
            StockMovementType.Receipt or
            StockMovementType.Return or
            StockMovementType.OpeningBalance when quantity > 0 => quantity,

            StockMovementType.Issue when quantity > 0 => checked(-quantity),
            StockMovementType.Adjustment when quantity != 0 => quantity,

            // Historical rows may contain the old location-only transfer. It
            // never changed quantity, so it remains readable as a zero delta.
            StockMovementType.Transfer => 0,

            StockMovementType.OpeningBalance => throw new BusinessRuleViolationException(
                "Opening balance quantity must be greater than zero."),

            _ => throw new BusinessRuleViolationException(
                "Movement quantity must be non-zero and positive unless the movement is an adjustment.")
        };
    }

    /// <summary>
    /// Applies a supported single-location movement to <paramref name="product"/>
    /// and returns the matching immutable ledger entry. Transfer is deliberately
    /// excluded until a per-location balance model exists.
    /// </summary>
    public static StockMovement Post(
        Product product,
        Guid operationId,
        StockMovementType type,
        int quantity,
        string reason,
        string? reference,
        Guid performedById,
        Guid? workOrderId = null,
        DateTime? timestampUtc = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (operationId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("A non-empty operation id is required.");
        }

        if (performedById == Guid.Empty)
        {
            throw new BusinessRuleViolationException("A movement actor is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("A movement reason is required.");
        }

        if (type == StockMovementType.Transfer)
        {
            throw new BusinessRuleViolationException(
                "Transfer is not supported by the single-location inventory model. See RFC-0001.");
        }

        var delta = CalculateQuantityDelta(type, quantity);
        var balanceBefore = product.CurrentStock;
        product.ApplyStockDelta(delta);

        var (source, destination) = type switch
        {
            StockMovementType.Receipt or StockMovementType.Return =>
                (ExternalLocation, product.Location),
            StockMovementType.Issue =>
                (product.Location, ExternalLocation),
            StockMovementType.Adjustment =>
                (product.Location, product.Location),
            StockMovementType.OpeningBalance =>
                (OpeningBalanceSource, product.Location),
            _ => throw new BusinessRuleViolationException(
                "Unsupported movement type for the single-location inventory model.")
        };

        return new StockMovement
        {
            OperationId = operationId,
            ProductId = product.Id,
            Product = product,
            Type = type,
            Quantity = quantity,
            BalanceBefore = balanceBefore,
            BalanceAfter = product.CurrentStock,
            SourceLocation = source,
            DestinationLocation = destination,
            Reason = reason.Trim(),
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            WorkOrderId = workOrderId,
            PerformedById = performedById,
            Timestamp = timestampUtc ?? DateTime.UtcNow,
            UnitCostAtTransaction = product.UnitCost
        };
    }
}

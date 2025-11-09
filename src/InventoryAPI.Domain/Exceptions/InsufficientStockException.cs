namespace InventoryAPI.Domain.Exceptions;

/// <summary>
/// Exception for insufficient stock operations
/// </summary>
public class InsufficientStockException : BusinessRuleViolationException
{
    public Guid ProductId { get; }
    public int Available { get; }
    public int Requested { get; }

    public InsufficientStockException(Guid productId, int available, int requested)
        : base($"Insufficient stock for product {productId}. Available: {available}, Requested: {requested}")
    {
        ProductId = productId;
        Available = available;
        Requested = requested;
    }
}

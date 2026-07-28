namespace InventoryAPI.Domain.Exceptions;

/// <summary>
/// Raised when an idempotency key is reused for a materially different stock
/// operation. Reusing a key for an equivalent operation is a safe replay.
/// </summary>
public sealed class IdempotencyConflictException : DomainException
{
    public Guid OperationId { get; }

    public IdempotencyConflictException(Guid operationId)
        : base($"Operation id {operationId} has already been used for a different stock operation.")
    {
        OperationId = operationId;
    }
}

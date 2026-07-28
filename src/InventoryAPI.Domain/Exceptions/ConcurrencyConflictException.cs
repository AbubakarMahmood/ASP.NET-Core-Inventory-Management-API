namespace InventoryAPI.Domain.Exceptions;

/// <summary>
/// Raised when a caller acts on a stale version or a concurrent database write
/// wins. The API maps this to HTTP 409 so callers can refresh and retry.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

namespace InventoryAPI.Domain.Exceptions;

/// <summary>
/// Exception for business rule violations
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException() : base()
    {
    }

    public BusinessRuleViolationException(string message) : base(message)
    {
    }

    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

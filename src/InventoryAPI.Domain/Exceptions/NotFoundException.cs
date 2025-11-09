namespace InventoryAPI.Domain.Exceptions;

/// <summary>
/// Exception for when an entity is not found
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException() : base()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}

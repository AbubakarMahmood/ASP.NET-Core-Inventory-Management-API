namespace InventoryAPI.Domain.Common;

/// <summary>
/// Base entity with common properties
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Optimistic concurrency token. Mapped to PostgreSQL's xmin system column,
    /// which the database updates automatically on every write.
    /// </summary>
    public uint Version { get; set; }
}

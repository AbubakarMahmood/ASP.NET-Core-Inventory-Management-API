using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Repository interface for generic CRUD operations
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Unit of Work pattern for coordinating repository changes.
/// A single <see cref="SaveChangesAsync"/> call is atomic; use
/// <see cref="ExecuteInTransactionAsync"/> when multiple save points must
/// commit or roll back together.
/// </summary>
public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<Product> Products { get; }
    IWorkOrderRepository WorkOrders { get; }
    IRepository<WorkOrderItem> WorkOrderItems { get; }
    IRepository<StockMovement> StockMovements { get; }
    IRepository<FilterPreset> FilterPresets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction that is
    /// compatible with the configured retrying execution strategy.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}

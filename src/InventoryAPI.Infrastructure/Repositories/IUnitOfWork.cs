using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Infrastructure.Repositories;

/// <summary>
/// Unit of Work pattern for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Product> Products { get; }
    IRepository<WorkOrder> WorkOrders { get; }
    IRepository<WorkOrderItem> WorkOrderItems { get; }
    IRepository<StockMovement> StockMovements { get; }
    IRepository<FilterPreset> FilterPresets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

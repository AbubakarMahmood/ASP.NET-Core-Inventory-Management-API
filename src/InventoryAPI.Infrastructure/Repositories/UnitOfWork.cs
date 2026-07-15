using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation over the EF Core DbContext.
/// The context is owned and disposed by the DI container.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new Repository<User>(_context);
        Products = new Repository<Product>(_context);
        WorkOrders = new WorkOrderRepository(_context);
        WorkOrderItems = new Repository<WorkOrderItem>(_context);
        StockMovements = new Repository<StockMovement>(_context);
        FilterPresets = new Repository<FilterPreset>(_context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Product> Products { get; }
    public IWorkOrderRepository WorkOrders { get; }
    public IRepository<WorkOrderItem> WorkOrderItems { get; }
    public IRepository<StockMovement> StockMovements { get; }
    public IRepository<FilterPreset> FilterPresets { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the operation inside an explicit transaction. Wrapped in the
    /// execution strategy because the context is configured with
    /// EnableRetryOnFailure, which does not allow user-initiated transactions
    /// outside of it.
    /// </summary>
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await operation(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}

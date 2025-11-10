using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace InventoryAPI.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new Repository<User>(_context);
        Products = new Repository<Product>(_context);
        WorkOrders = new Repository<WorkOrder>(_context);
        WorkOrderItems = new Repository<WorkOrderItem>(_context);
        StockMovements = new Repository<StockMovement>(_context);
        FilterPresets = new Repository<FilterPreset>(_context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Product> Products { get; }
    public IRepository<WorkOrder> WorkOrders { get; }
    public IRepository<WorkOrderItem> WorkOrderItems { get; }
    public IRepository<StockMovement> StockMovements { get; }
    public IRepository<FilterPreset> FilterPresets { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

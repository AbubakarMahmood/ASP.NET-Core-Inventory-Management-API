using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Application database context interface
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    DbSet<WorkOrder> WorkOrders { get; }
    DbSet<WorkOrderItem> WorkOrderItems { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<FilterPreset> FilterPresets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

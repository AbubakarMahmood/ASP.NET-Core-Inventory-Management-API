using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Infrastructure.Repositories;

/// <summary>
/// WorkOrder repository with specialized query methods
/// </summary>
public class WorkOrderRepository : Repository<WorkOrder>, IWorkOrderRepository
{
    public WorkOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<WorkOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(w => w.Items)
            .Include(w => w.RequestedBy)
            .Include(w => w.AssignedTo)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }
}

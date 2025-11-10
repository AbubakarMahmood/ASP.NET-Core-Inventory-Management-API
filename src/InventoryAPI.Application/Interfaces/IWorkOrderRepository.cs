using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Extended repository interface for WorkOrder with specialized query methods
/// </summary>
public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<WorkOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

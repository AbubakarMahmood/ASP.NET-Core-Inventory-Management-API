using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.WorkOrders;

/// <summary>
/// Query to get a single work order by ID with all details
/// </summary>
public class GetWorkOrderByIdQuery : IRequest<WorkOrderDto?>
{
    public Guid WorkOrderId { get; set; }

    public GetWorkOrderByIdQuery(Guid workOrderId)
    {
        WorkOrderId = workOrderId;
    }
}

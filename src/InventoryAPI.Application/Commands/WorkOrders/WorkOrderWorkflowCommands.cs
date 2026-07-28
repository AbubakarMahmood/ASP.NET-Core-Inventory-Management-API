using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.WorkOrders;

public class SubmitWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

public class ApproveWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
    public Guid AssignedToId { get; set; }
}

public class RejectWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class StartWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

public class CompleteWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

public class CancelWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

public class IssueWorkOrderItemsCommand : IRequest<WorkOrderDto>
{
    /// <summary>
    /// Stable caller-generated id shared by every movement in this issue batch.
    /// Equivalent retries are safe; a different payload with the same id conflicts.
    /// </summary>
    public Guid OperationId { get; set; }

    public Guid WorkOrderId { get; set; }
    public List<IssueItemRequest> Items { get; set; } = new();
}

public class IssueItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.WorkOrders;

/// <summary>
/// Submit a draft work order for approval
/// </summary>
public class SubmitWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

/// <summary>
/// Approve a submitted work order
/// </summary>
public class ApproveWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
    public Guid AssignedToId { get; set; }
}

/// <summary>
/// Reject a submitted work order
/// </summary>
public class RejectWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Start work on an approved work order
/// </summary>
public class StartWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

/// <summary>
/// Complete an in-progress work order
/// </summary>
public class CompleteWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

/// <summary>
/// Cancel a work order
/// </summary>
public class CancelWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
}

/// <summary>
/// Issue items from a work order (creates stock movements)
/// </summary>
public class IssueWorkOrderItemsCommand : IRequest<WorkOrderDto>
{
    public Guid WorkOrderId { get; set; }
    public List<IssueItemRequest> Items { get; set; } = new();
}

public class IssueItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? FromLocation { get; set; }
    public string? Notes { get; set; }
}

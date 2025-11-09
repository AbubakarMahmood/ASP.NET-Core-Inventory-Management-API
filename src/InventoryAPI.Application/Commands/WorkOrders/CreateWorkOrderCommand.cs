using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Commands.WorkOrders;

/// <summary>
/// Command to create a new work order
/// </summary>
public class CreateWorkOrderCommand : IRequest<WorkOrderDto>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public DateTime? DueDate { get; set; }
    public List<CreateWorkOrderItemRequest> Items { get; set; } = new();
}

public class CreateWorkOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int QuantityRequested { get; set; }
    public string? Notes { get; set; }
}

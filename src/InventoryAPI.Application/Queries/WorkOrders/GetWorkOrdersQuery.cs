using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Queries.WorkOrders;

/// <summary>
/// Query to get work orders with filtering, pagination, and multi-column sorting
/// </summary>
public class GetWorkOrdersQuery : IRequest<PaginatedResult<WorkOrderDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public WorkOrderStatus? Status { get; set; }
    public WorkOrderPriority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid? RequestedById { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // Multi-column sorting (comma-separated: "OrderNumber,Priority,CreatedAt")
    public string? SortBy { get; set; }
    // Sort order for each column (comma-separated: "asc,desc,desc")
    public string? SortOrder { get; set; }
}

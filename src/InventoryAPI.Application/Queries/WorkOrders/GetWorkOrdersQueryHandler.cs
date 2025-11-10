using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Common;
using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.WorkOrders;

/// <summary>
/// Handler for getting work orders with filtering
/// </summary>
public class GetWorkOrdersQueryHandler : IRequestHandler<GetWorkOrdersQuery, PaginatedResult<WorkOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetWorkOrdersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<WorkOrderDto>> Handle(GetWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WorkOrders
            .Include(w => w.RequestedBy)
            .Include(w => w.AssignedTo)
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(w => w.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(w => w.Priority == request.Priority.Value);
        }

        if (request.AssignedToId.HasValue)
        {
            query = query.Where(w => w.AssignedToId == request.AssignedToId.Value);
        }

        if (request.RequestedById.HasValue)
        {
            query = query.Where(w => w.RequestedById == request.RequestedById.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= request.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Multi-column sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        // Apply pagination
        var workOrders = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var workOrderDtos = new List<WorkOrderDto>();
        foreach (var workOrder in workOrders)
        {
            var dto = _mapper.Map<WorkOrderDto>(workOrder);
            dto.RequestedByName = workOrder.RequestedBy?.FullName ?? "Unknown";
            dto.RequestedByEmail = workOrder.RequestedBy?.Email ?? "";
            dto.AssignedToName = workOrder.AssignedTo?.FullName;
            dto.AssignedToEmail = workOrder.AssignedTo?.Email;

            dto.Items = workOrder.Items.Select(item => new WorkOrderItemDto
            {
                Id = item.Id,
                WorkOrderId = item.WorkOrderId,
                ProductId = item.ProductId,
                ProductSKU = item.Product?.SKU ?? "",
                ProductName = item.Product?.Name ?? "",
                UnitOfMeasure = item.Product?.UnitOfMeasure ?? "",
                CurrentStock = item.Product?.CurrentStock ?? 0,
                QuantityRequested = item.QuantityRequested,
                QuantityIssued = item.QuantityIssued,
                Notes = item.Notes
            }).ToList();

            workOrderDtos.Add(dto);
        }

        return new PaginatedResult<WorkOrderDto>(
            workOrderDtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }

    private IQueryable<Domain.Entities.WorkOrder> ApplySorting(
        IQueryable<Domain.Entities.WorkOrder> query,
        string? sortBy,
        string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query.OrderByDescending(w => w.CreatedAt); // Default sorting
        }

        var sortColumns = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var sortOrders = sortOrder?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        IOrderedQueryable<Domain.Entities.WorkOrder>? orderedQuery = null;

        for (int i = 0; i < sortColumns.Length; i++)
        {
            var column = sortColumns[i].Trim();
            var order = i < sortOrders.Length ? sortOrders[i].Trim().ToLower() : "asc";
            var isDescending = order == "desc";

            if (orderedQuery == null)
            {
                orderedQuery = ApplyOrderBy(query, column, isDescending);
            }
            else
            {
                orderedQuery = ApplyThenBy(orderedQuery, column, isDescending);
            }
        }

        return orderedQuery ?? query.OrderByDescending(w => w.CreatedAt);
    }

    private IOrderedQueryable<Domain.Entities.WorkOrder> ApplyOrderBy(
        IQueryable<Domain.Entities.WorkOrder> query,
        string column,
        bool descending)
    {
        return column.ToLower() switch
        {
            "ordernumber" => descending ? query.OrderByDescending(w => w.OrderNumber) : query.OrderBy(w => w.OrderNumber),
            "status" => descending ? query.OrderByDescending(w => w.Status) : query.OrderBy(w => w.Status),
            "priority" => descending ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
            "createdat" => descending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt),
            "requesteddate" => descending ? query.OrderByDescending(w => w.RequestedDate) : query.OrderBy(w => w.RequestedDate),
            "completeddate" => descending ? query.OrderByDescending(w => w.CompletedDate) : query.OrderBy(w => w.CompletedDate),
            _ => descending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
        };
    }

    private IOrderedQueryable<Domain.Entities.WorkOrder> ApplyThenBy(
        IOrderedQueryable<Domain.Entities.WorkOrder> query,
        string column,
        bool descending)
    {
        return column.ToLower() switch
        {
            "ordernumber" => descending ? query.ThenByDescending(w => w.OrderNumber) : query.ThenBy(w => w.OrderNumber),
            "status" => descending ? query.ThenByDescending(w => w.Status) : query.ThenBy(w => w.Status),
            "priority" => descending ? query.ThenByDescending(w => w.Priority) : query.ThenBy(w => w.Priority),
            "createdat" => descending ? query.ThenByDescending(w => w.CreatedAt) : query.ThenBy(w => w.CreatedAt),
            "requesteddate" => descending ? query.ThenByDescending(w => w.RequestedDate) : query.ThenBy(w => w.RequestedDate),
            "completeddate" => descending ? query.ThenByDescending(w => w.CompletedDate) : query.ThenBy(w => w.CompletedDate),
            _ => descending ? query.ThenByDescending(w => w.CreatedAt) : query.ThenBy(w => w.CreatedAt)
        };
    }
}

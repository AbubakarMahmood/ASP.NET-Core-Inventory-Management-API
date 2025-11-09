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
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetWorkOrdersQueryHandler(ApplicationDbContext context, IMapper mapper)
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

        // Apply pagination and ordering
        var workOrders = await query
            .OrderByDescending(w => w.CreatedAt)
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
}

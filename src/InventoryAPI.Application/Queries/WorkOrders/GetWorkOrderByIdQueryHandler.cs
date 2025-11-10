using InventoryAPI.Application.Interfaces;
using AutoMapper;
using InventoryAPI.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.WorkOrders;

/// <summary>
/// Handler for getting a work order by ID
/// </summary>
public class GetWorkOrderByIdQueryHandler : IRequestHandler<GetWorkOrderByIdQuery, WorkOrderDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetWorkOrderByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<WorkOrderDto?> Handle(GetWorkOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var workOrder = await _context.WorkOrders
            .Include(w => w.RequestedBy)
            .Include(w => w.AssignedTo)
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(w => w.Id == request.WorkOrderId, cancellationToken);

        if (workOrder == null)
        {
            return null;
        }

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

        return dto;
    }
}

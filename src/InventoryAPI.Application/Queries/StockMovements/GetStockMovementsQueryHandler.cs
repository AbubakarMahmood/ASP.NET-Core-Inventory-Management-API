using AutoMapper;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.StockMovements;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, PaginatedResult<StockMovementDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStockMovementsQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.PerformedBy)
            .Include(sm => sm.WorkOrder)
            .AsQueryable();

        // Apply filters
        if (request.ProductId.HasValue)
        {
            query = query.Where(sm => sm.ProductId == request.ProductId.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(sm => sm.Type == request.Type.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(sm => sm.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(sm => sm.Timestamp <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(sm => sm.Timestamp)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var movementDtos = movements.Select(m => new StockMovementDto
        {
            Id = m.Id,
            ProductId = m.ProductId,
            ProductSKU = m.Product.SKU,
            ProductName = m.Product.Name,
            Type = m.Type,
            Quantity = m.Quantity,
            SourceLocation = m.SourceLocation,
            DestinationLocation = m.DestinationLocation,
            Reason = m.Reason,
            Reference = m.Reference,
            WorkOrderId = m.WorkOrderId,
            WorkOrderNumber = m.WorkOrder?.OrderNumber,
            PerformedById = m.PerformedById,
            PerformedByName = m.PerformedBy.FullName,
            Timestamp = m.Timestamp,
            UnitCostAtTransaction = m.UnitCostAtTransaction
        }).ToList();

        return new PaginatedResult<StockMovementDto>(
            movementDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}

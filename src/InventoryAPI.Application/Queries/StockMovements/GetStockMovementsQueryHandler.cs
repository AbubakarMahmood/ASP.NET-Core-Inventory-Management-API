using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.StockMovements;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, PaginatedResult<StockMovementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockMovementsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<StockMovementDto>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockMovements
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(movement => movement.Product)
            .Include(movement => movement.PerformedBy)
            .Include(movement => movement.WorkOrder)
            .AsQueryable();

        if (request.ProductId.HasValue)
        {
            query = query.Where(movement => movement.ProductId == request.ProductId.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(movement => movement.Type == request.Type.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(movement => movement.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(movement => movement.Timestamp <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var movements = await query
            .OrderByDescending(movement => movement.Timestamp)
            .ThenByDescending(movement => movement.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<StockMovementDto>(
            movements.Select(movement => movement.ToDto()).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}

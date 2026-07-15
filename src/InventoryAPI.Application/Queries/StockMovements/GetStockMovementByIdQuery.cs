using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Queries.StockMovements;

public class GetStockMovementByIdQuery : IRequest<StockMovementDto>
{
    public Guid Id { get; set; }
}

public class GetStockMovementByIdQueryHandler : IRequestHandler<GetStockMovementByIdQuery, StockMovementDto>
{
    private readonly IApplicationDbContext _context;

    public GetStockMovementByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockMovementDto> Handle(GetStockMovementByIdQuery request, CancellationToken cancellationToken)
    {
        var movement = await _context.StockMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.PerformedBy)
            .Include(m => m.WorkOrder)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StockMovement), request.Id);

        return movement.ToDto();
    }
}

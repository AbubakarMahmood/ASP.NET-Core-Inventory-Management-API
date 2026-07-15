using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Exceptions;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Queries.Products;

/// <summary>
/// Get product by ID query handler
/// </summary>
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException($"Product with ID {request.Id} not found");
        }

        return product.ToDto();
    }
}

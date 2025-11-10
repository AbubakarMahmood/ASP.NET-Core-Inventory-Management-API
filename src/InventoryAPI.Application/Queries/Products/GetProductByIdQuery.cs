using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.Products;

/// <summary>
/// Get product by ID query
/// </summary>
public class GetProductByIdQuery : IRequest<ProductDto>
{
    public Guid Id { get; set; }
}

using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.Products;

/// <summary>
/// Get all products query with pagination
/// </summary>
public class GetProductsQuery : IRequest<PaginatedResult<ProductDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public bool? LowStockOnly { get; set; }
}

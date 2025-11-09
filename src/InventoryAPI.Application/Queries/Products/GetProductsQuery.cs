using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.Products;

/// <summary>
/// Get all products query with pagination, advanced filtering, and multi-column sorting
/// </summary>
public class GetProductsQuery : IRequest<PaginatedResult<ProductDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public bool? LowStockOnly { get; set; }

    // Advanced filtering with operators
    public decimal? UnitCostMin { get; set; }
    public decimal? UnitCostMax { get; set; }
    public int? CurrentStockMin { get; set; }
    public int? CurrentStockMax { get; set; }
    public int? ReorderPointMin { get; set; }
    public int? ReorderPointMax { get; set; }

    // Multi-column sorting (comma-separated: "Name,UnitCost")
    public string? SortBy { get; set; }
    // Sort order for each column (comma-separated: "asc,desc")
    public string? SortOrder { get; set; }
}

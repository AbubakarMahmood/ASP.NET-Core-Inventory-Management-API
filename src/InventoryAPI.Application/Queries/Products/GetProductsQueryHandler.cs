using AutoMapper;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.Products;

/// <summary>
/// Get products query handler
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(request.SearchTerm) ||
                p.SKU.Contains(request.SearchTerm) ||
                p.Description.Contains(request.SearchTerm));
        }

        if (request.LowStockOnly == true)
        {
            query = query.Where(p => p.CurrentStock <= p.ReorderPoint);
        }

        // Advanced filtering with operators
        if (request.UnitCostMin.HasValue)
        {
            query = query.Where(p => p.UnitCost >= request.UnitCostMin.Value);
        }

        if (request.UnitCostMax.HasValue)
        {
            query = query.Where(p => p.UnitCost <= request.UnitCostMax.Value);
        }

        if (request.CurrentStockMin.HasValue)
        {
            query = query.Where(p => p.CurrentStock >= request.CurrentStockMin.Value);
        }

        if (request.CurrentStockMax.HasValue)
        {
            query = query.Where(p => p.CurrentStock <= request.CurrentStockMax.Value);
        }

        if (request.ReorderPointMin.HasValue)
        {
            query = query.Where(p => p.ReorderPoint >= request.ReorderPointMin.Value);
        }

        if (request.ReorderPointMax.HasValue)
        {
            query = query.Where(p => p.ReorderPoint <= request.ReorderPointMax.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Multi-column sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return new PaginatedResult<ProductDto>(
            productDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    private IQueryable<Domain.Entities.Product> ApplySorting(
        IQueryable<Domain.Entities.Product> query,
        string? sortBy,
        string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query.OrderBy(p => p.Name); // Default sorting
        }

        var sortColumns = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var sortOrders = sortOrder?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        IOrderedQueryable<Domain.Entities.Product>? orderedQuery = null;

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

        return orderedQuery ?? query.OrderBy(p => p.Name);
    }

    private IOrderedQueryable<Domain.Entities.Product> ApplyOrderBy(
        IQueryable<Domain.Entities.Product> query,
        string column,
        bool descending)
    {
        return column.ToLower() switch
        {
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "sku" => descending ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
            "category" => descending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
            "unitcost" => descending ? query.OrderByDescending(p => p.UnitCost) : query.OrderBy(p => p.UnitCost),
            "currentstock" => descending ? query.OrderByDescending(p => p.CurrentStock) : query.OrderBy(p => p.CurrentStock),
            "reorderpoint" => descending ? query.OrderByDescending(p => p.ReorderPoint) : query.OrderBy(p => p.ReorderPoint),
            "createdat" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };
    }

    private IOrderedQueryable<Domain.Entities.Product> ApplyThenBy(
        IOrderedQueryable<Domain.Entities.Product> query,
        string column,
        bool descending)
    {
        return column.ToLower() switch
        {
            "name" => descending ? query.ThenByDescending(p => p.Name) : query.ThenBy(p => p.Name),
            "sku" => descending ? query.ThenByDescending(p => p.SKU) : query.ThenBy(p => p.SKU),
            "category" => descending ? query.ThenByDescending(p => p.Category) : query.ThenBy(p => p.Category),
            "unitcost" => descending ? query.ThenByDescending(p => p.UnitCost) : query.ThenBy(p => p.UnitCost),
            "currentstock" => descending ? query.ThenByDescending(p => p.CurrentStock) : query.ThenBy(p => p.CurrentStock),
            "reorderpoint" => descending ? query.ThenByDescending(p => p.ReorderPoint) : query.ThenBy(p => p.ReorderPoint),
            "createdat" => descending ? query.ThenByDescending(p => p.CreatedAt) : query.ThenBy(p => p.CreatedAt),
            _ => descending ? query.ThenByDescending(p => p.Name) : query.ThenBy(p => p.Name)
        };
    }
}

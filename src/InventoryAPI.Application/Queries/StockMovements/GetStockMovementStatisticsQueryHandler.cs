using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.StockMovements;

/// <summary>
/// Computes stock movement statistics with database-side aggregation
/// </summary>
public class GetStockMovementStatisticsQueryHandler
    : IRequestHandler<GetStockMovementStatisticsQuery, StockMovementStatisticsDto>
{
    private readonly IApplicationDbContext _context;

    public GetStockMovementStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockMovementStatisticsDto> Handle(
        GetStockMovementStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockMovements.AsNoTracking().AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(m => m.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(m => m.Timestamp <= request.ToDate.Value);
        }

        var byType = await query
            .GroupBy(m => m.Type)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count(),
                Quantity = g.Sum(m => m.Quantity)
            })
            .ToListAsync(cancellationToken);

        var result = new StockMovementStatisticsDto
        {
            TotalMovements = byType.Sum(g => g.Count),
            ReceiptCount = CountOf(StockMovementType.Receipt),
            IssueCount = CountOf(StockMovementType.Issue),
            AdjustmentCount = CountOf(StockMovementType.Adjustment),
            TransferCount = CountOf(StockMovementType.Transfer),
            ReturnCount = CountOf(StockMovementType.Return),
            TotalQuantityIn = QuantityOf(StockMovementType.Receipt) + QuantityOf(StockMovementType.Return),
            TotalQuantityOut = QuantityOf(StockMovementType.Issue),
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        if (result.TotalMovements > 0)
        {
            result.UniqueProducts = await query
                .Select(m => m.ProductId)
                .Distinct()
                .CountAsync(cancellationToken);

            result.FromDate ??= await query.MinAsync(m => m.Timestamp, cancellationToken);
            result.ToDate ??= await query.MaxAsync(m => m.Timestamp, cancellationToken);
        }

        return result;

        int CountOf(StockMovementType type) => byType.FirstOrDefault(g => g.Type == type)?.Count ?? 0;
        int QuantityOf(StockMovementType type) => byType.FirstOrDefault(g => g.Type == type)?.Quantity ?? 0;
    }
}

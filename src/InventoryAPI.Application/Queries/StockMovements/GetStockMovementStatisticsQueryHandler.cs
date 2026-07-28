using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.StockMovements;

/// <summary>
/// Computes signed ledger statistics over an optional UTC date range.
/// Historical transfer rows are counted separately and contribute no quantity.
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
        var query = _context.StockMovements
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(movement => movement.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(movement => movement.Timestamp <= request.ToDate.Value);
        }

        var byType = await query
            .GroupBy(movement => movement.Type)
            .Select(group => new
            {
                Type = group.Key,
                Count = group.Count(),
                Quantity = group.Sum(movement => (long)movement.Quantity)
            })
            .ToListAsync(cancellationToken);

        var positiveAdjustments = await query
            .Where(movement => movement.Type == StockMovementType.Adjustment && movement.Quantity > 0)
            .SumAsync(movement => (long?)movement.Quantity, cancellationToken) ?? 0;
        var negativeAdjustments = await query
            .Where(movement => movement.Type == StockMovementType.Adjustment && movement.Quantity < 0)
            .SumAsync(movement => (long?)(-(long)movement.Quantity), cancellationToken) ?? 0;

        var quantityIn =
            QuantityOf(StockMovementType.OpeningBalance) +
            QuantityOf(StockMovementType.Receipt) +
            QuantityOf(StockMovementType.Return) +
            positiveAdjustments;
        var quantityOut = QuantityOf(StockMovementType.Issue) + negativeAdjustments;

        var result = new StockMovementStatisticsDto
        {
            TotalMovements = byType.Sum(group => group.Count),
            OpeningBalanceCount = CountOf(StockMovementType.OpeningBalance),
            ReceiptCount = CountOf(StockMovementType.Receipt),
            IssueCount = CountOf(StockMovementType.Issue),
            AdjustmentCount = CountOf(StockMovementType.Adjustment),
            LegacyTransferCount = CountOf(StockMovementType.Transfer),
            ReturnCount = CountOf(StockMovementType.Return),
            TotalQuantityIn = quantityIn,
            TotalQuantityOut = quantityOut,
            NetQuantityChange = quantityIn - quantityOut,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        if (result.TotalMovements > 0)
        {
            result.UniqueProducts = await query
                .Select(movement => movement.ProductId)
                .Distinct()
                .CountAsync(cancellationToken);

            result.FromDate ??= await query.MinAsync(movement => movement.Timestamp, cancellationToken);
            result.ToDate ??= await query.MaxAsync(movement => movement.Timestamp, cancellationToken);
        }

        return result;

        int CountOf(StockMovementType type) =>
            byType.FirstOrDefault(group => group.Type == type)?.Count ?? 0;

        long QuantityOf(StockMovementType type) =>
            byType.FirstOrDefault(group => group.Type == type)?.Quantity ?? 0;
    }
}

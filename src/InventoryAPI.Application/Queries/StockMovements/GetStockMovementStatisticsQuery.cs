using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.StockMovements;

public class GetStockMovementStatisticsQuery : IRequest<StockMovementStatisticsDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

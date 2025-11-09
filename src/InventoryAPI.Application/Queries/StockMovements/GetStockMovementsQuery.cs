using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Queries.StockMovements;

public class GetStockMovementsQuery : IRequest<PaginatedResult<StockMovementDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? ProductId { get; set; }
    public StockMovementType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

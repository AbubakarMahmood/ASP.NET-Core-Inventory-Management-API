using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Commands.StockMovements;

public class RecordStockMovementCommand : IRequest<StockMovementDto>
{
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public Guid? WorkOrderId { get; set; }
}

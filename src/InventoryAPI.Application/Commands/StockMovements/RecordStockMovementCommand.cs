using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Commands.StockMovements;

/// <summary>
/// Posts one manual movement to the append-only ledger. Locations are derived
/// from the product's single-location model; work-order issues use their
/// dedicated workflow endpoint.
/// </summary>
public class RecordStockMovementCommand : IRequest<StockMovementDto>
{
    /// <summary>
    /// Stable caller-generated id used to make retries safe. Replaying an
    /// equivalent request returns the original ledger row.
    /// </summary>
    public Guid OperationId { get; set; }

    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

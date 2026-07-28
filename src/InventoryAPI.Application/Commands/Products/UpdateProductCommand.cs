using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Updates descriptive and replenishment metadata. On-hand stock is deliberately
/// absent: every balance change must be posted through the stock ledger.
/// </summary>
public class UpdateProductCommand : IRequest<ProductDto>
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// PostgreSQL xmin value returned by the read endpoint. It is required so a
    /// stale edit cannot silently overwrite a concurrent change.
    /// </summary>
    public uint? Version { get; set; }
}

using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Creates a catalog product. Any non-zero opening quantity is posted as an
/// immutable OpeningBalance movement in the same database commit.
/// </summary>
public class CreateProductCommand : IRequest<ProductDto>
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int OpeningStock { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
}

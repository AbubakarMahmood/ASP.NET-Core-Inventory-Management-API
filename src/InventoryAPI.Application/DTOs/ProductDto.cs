namespace InventoryAPI.Application.DTOs;

/// <summary>
/// Product catalog data with the cached on-hand quantity derived from ledger
/// postings.
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsLowStock { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// PostgreSQL xmin concurrency token. Send it on product metadata updates.
    /// </summary>
    public uint Version { get; set; }
}

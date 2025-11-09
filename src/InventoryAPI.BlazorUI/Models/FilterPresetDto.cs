namespace InventoryAPI.BlazorUI.Models;

/// <summary>
/// DTO for filter preset
/// </summary>
public class FilterPresetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string FilterData { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

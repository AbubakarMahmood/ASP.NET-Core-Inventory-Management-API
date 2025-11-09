using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.FilterPresets;

/// <summary>
/// Get filter presets query with optional filtering
/// </summary>
public class GetFilterPresetsQuery : IRequest<List<FilterPresetDto>>
{
    public string? EntityType { get; set; }
    public bool? IncludeShared { get; set; } = true;
}

using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Create filter preset command
/// </summary>
public class CreateFilterPresetCommand : IRequest<FilterPresetDto>
{
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string FilterData { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
}

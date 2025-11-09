using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Update filter preset command
/// </summary>
public class UpdateFilterPresetCommand : IRequest<FilterPresetDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterData { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
}

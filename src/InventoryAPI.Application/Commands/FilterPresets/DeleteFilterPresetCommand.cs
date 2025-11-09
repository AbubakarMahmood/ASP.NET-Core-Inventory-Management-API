using MediatR;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Delete filter preset command
/// </summary>
public class DeleteFilterPresetCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}

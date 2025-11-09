using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.FilterPresets;

/// <summary>
/// Get filter preset by ID query
/// </summary>
public class GetFilterPresetByIdQuery : IRequest<FilterPresetDto>
{
    public Guid Id { get; set; }
}

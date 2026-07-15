using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Queries.FilterPresets;

/// <summary>
/// Get filter preset by ID query handler
/// </summary>
public class GetFilterPresetByIdQueryHandler : IRequestHandler<GetFilterPresetByIdQuery, FilterPresetDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetFilterPresetByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FilterPresetDto> Handle(GetFilterPresetByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var filterPreset = await _unitOfWork.FilterPresets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FilterPreset), request.Id);

        // Accessible when owned by the caller or explicitly shared
        if (filterPreset.UserId != userId && !filterPreset.IsShared)
        {
            throw new UnauthorizedAccessException("You don't have access to this filter preset");
        }

        return filterPreset.ToDto();
    }
}

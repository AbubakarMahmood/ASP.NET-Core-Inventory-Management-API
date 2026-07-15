using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Queries.FilterPresets;

/// <summary>
/// Get filter presets query handler
/// </summary>
public class GetFilterPresetsQueryHandler : IRequestHandler<GetFilterPresetsQuery, List<FilterPresetDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetFilterPresetsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<List<FilterPresetDto>> Handle(GetFilterPresetsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var presets = await _unitOfWork.FilterPresets.FindAsync(
            fp => fp.UserId == userId &&
                  (string.IsNullOrEmpty(request.EntityType) || fp.EntityType == request.EntityType),
            cancellationToken);

        var presetList = presets.ToList();

        if (request.IncludeShared == true)
        {
            var sharedPresets = await _unitOfWork.FilterPresets.FindAsync(
                fp => fp.UserId != userId &&
                      fp.IsShared &&
                      (string.IsNullOrEmpty(request.EntityType) || fp.EntityType == request.EntityType),
                cancellationToken);

            presetList.AddRange(sharedPresets);
        }

        var orderedPresets = presetList
            .OrderByDescending(fp => fp.IsDefault)
            .ThenBy(fp => fp.Name)
            .ToList();

        return orderedPresets.Select(x => x.ToDto()).ToList();
    }
}

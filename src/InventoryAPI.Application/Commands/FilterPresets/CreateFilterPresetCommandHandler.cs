using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Create filter preset command handler
/// </summary>
public class CreateFilterPresetCommandHandler : IRequestHandler<CreateFilterPresetCommand, FilterPresetDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateFilterPresetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FilterPresetDto> Handle(CreateFilterPresetCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        // A user has at most one default preset per entity type
        if (request.IsDefault)
        {
            var existingDefaults = await _unitOfWork.FilterPresets
                .FindAsync(fp => fp.UserId == userId && fp.EntityType == request.EntityType && fp.IsDefault, cancellationToken);

            foreach (var existingDefault in existingDefaults)
            {
                existingDefault.IsDefault = false;
                _unitOfWork.FilterPresets.Update(existingDefault);
            }
        }

        var filterPreset = new FilterPreset
        {
            UserId = userId,
            Name = request.Name,
            EntityType = request.EntityType,
            FilterData = request.FilterData,
            IsDefault = request.IsDefault,
            IsShared = request.IsShared,
            CreatedBy = _currentUser.Email ?? "Unknown"
        };

        await _unitOfWork.FilterPresets.AddAsync(filterPreset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return filterPreset.ToDto();
    }
}

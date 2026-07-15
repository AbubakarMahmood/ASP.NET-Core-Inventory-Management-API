using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Update filter preset command handler
/// </summary>
public class UpdateFilterPresetCommandHandler : IRequestHandler<UpdateFilterPresetCommand, FilterPresetDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateFilterPresetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FilterPresetDto> Handle(UpdateFilterPresetCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var filterPreset = await _unitOfWork.FilterPresets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FilterPreset), request.Id);

        if (filterPreset.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only update your own filter presets");
        }

        if (request.IsDefault && !filterPreset.IsDefault)
        {
            var existingDefaults = await _unitOfWork.FilterPresets
                .FindAsync(fp => fp.UserId == userId && fp.EntityType == filterPreset.EntityType && fp.IsDefault, cancellationToken);

            foreach (var existingDefault in existingDefaults)
            {
                existingDefault.IsDefault = false;
                _unitOfWork.FilterPresets.Update(existingDefault);
            }
        }

        filterPreset.Name = request.Name;
        filterPreset.FilterData = request.FilterData;
        filterPreset.IsDefault = request.IsDefault;
        filterPreset.IsShared = request.IsShared;
        filterPreset.ModifiedBy = _currentUser.Email;

        _unitOfWork.FilterPresets.Update(filterPreset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return filterPreset.ToDto();
    }
}

using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Update filter preset command handler
/// </summary>
public class UpdateFilterPresetCommandHandler : IRequestHandler<UpdateFilterPresetCommand, FilterPresetDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateFilterPresetCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<FilterPresetDto> Handle(UpdateFilterPresetCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var currentUserEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

        // Get existing filter preset
        var filterPreset = await _unitOfWork.FilterPresets.GetByIdAsync(request.Id, cancellationToken);
        if (filterPreset == null)
        {
            throw new NotFoundException(nameof(FilterPreset), request.Id);
        }

        // Verify ownership
        if (filterPreset.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only update your own filter presets");
        }

        // If this is set as default, unset other defaults for this entity type
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

        // Update properties
        filterPreset.Name = request.Name;
        filterPreset.FilterData = request.FilterData;
        filterPreset.IsDefault = request.IsDefault;
        filterPreset.IsShared = request.IsShared;
        filterPreset.ModifiedBy = currentUserEmail;

        _unitOfWork.FilterPresets.Update(filterPreset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FilterPresetDto>(filterPreset);
    }
}

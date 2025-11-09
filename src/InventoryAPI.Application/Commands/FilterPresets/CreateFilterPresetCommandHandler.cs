using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Create filter preset command handler
/// </summary>
public class CreateFilterPresetCommandHandler : IRequestHandler<CreateFilterPresetCommand, FilterPresetDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateFilterPresetCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<FilterPresetDto> Handle(CreateFilterPresetCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var currentUserEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

        // If this is set as default, unset other defaults for this entity type
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

        // Create filter preset
        var filterPreset = new FilterPreset
        {
            UserId = userId,
            Name = request.Name,
            EntityType = request.EntityType,
            FilterData = request.FilterData,
            IsDefault = request.IsDefault,
            IsShared = request.IsShared,
            CreatedBy = currentUserEmail
        };

        await _unitOfWork.FilterPresets.AddAsync(filterPreset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FilterPresetDto>(filterPreset);
    }
}

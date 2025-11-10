using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InventoryAPI.Application.Queries.FilterPresets;

/// <summary>
/// Get filter presets query handler
/// </summary>
public class GetFilterPresetsQueryHandler : IRequestHandler<GetFilterPresetsQuery, List<FilterPresetDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetFilterPresetsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<FilterPresetDto>> Handle(GetFilterPresetsQuery request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT claims
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Get user's own presets
        var presets = await _unitOfWork.FilterPresets.FindAsync(
            fp => fp.UserId == userId &&
                  (string.IsNullOrEmpty(request.EntityType) || fp.EntityType == request.EntityType),
            cancellationToken);

        var presetList = presets.ToList();

        // Include shared presets if requested
        if (request.IncludeShared == true)
        {
            var sharedPresets = await _unitOfWork.FilterPresets.FindAsync(
                fp => fp.UserId != userId &&
                      fp.IsShared &&
                      (string.IsNullOrEmpty(request.EntityType) || fp.EntityType == request.EntityType),
                cancellationToken);

            presetList.AddRange(sharedPresets);
        }

        // Order by: default first, then by name
        var orderedPresets = presetList
            .OrderByDescending(fp => fp.IsDefault)
            .ThenBy(fp => fp.Name)
            .ToList();

        return _mapper.Map<List<FilterPresetDto>>(orderedPresets);
    }
}

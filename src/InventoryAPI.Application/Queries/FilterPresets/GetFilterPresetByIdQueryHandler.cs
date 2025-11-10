using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InventoryAPI.Application.Queries.FilterPresets;

/// <summary>
/// Get filter preset by ID query handler
/// </summary>
public class GetFilterPresetByIdQueryHandler : IRequestHandler<GetFilterPresetByIdQuery, FilterPresetDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetFilterPresetByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<FilterPresetDto> Handle(GetFilterPresetByIdQuery request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT claims
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var filterPreset = await _unitOfWork.FilterPresets.GetByIdAsync(request.Id, cancellationToken);
        if (filterPreset == null)
        {
            throw new NotFoundException(nameof(FilterPreset), request.Id);
        }

        // Verify access (owner or shared)
        if (filterPreset.UserId != userId && !filterPreset.IsShared)
        {
            throw new UnauthorizedAccessException("You don't have access to this filter preset");
        }

        return _mapper.Map<FilterPresetDto>(filterPreset);
    }
}

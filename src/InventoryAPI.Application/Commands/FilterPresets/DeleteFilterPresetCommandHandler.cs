using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Delete filter preset command handler
/// </summary>
public class DeleteFilterPresetCommandHandler : IRequestHandler<DeleteFilterPresetCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteFilterPresetCommandHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Unit> Handle(DeleteFilterPresetCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT claims
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Get existing filter preset
        var filterPreset = await _unitOfWork.FilterPresets.GetByIdAsync(request.Id, cancellationToken);
        if (filterPreset == null)
        {
            throw new NotFoundException(nameof(FilterPreset), request.Id);
        }

        // Verify ownership
        if (filterPreset.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own filter presets");
        }

        _unitOfWork.FilterPresets.Remove(filterPreset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

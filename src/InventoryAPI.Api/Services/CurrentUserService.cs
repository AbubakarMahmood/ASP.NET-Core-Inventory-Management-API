using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryAPI.Application.Interfaces;

namespace InventoryAPI.Api.Services;

/// <summary>
/// Resolves the current user from the HTTP context's JWT claims.
/// Handles both raw JWT claim names and the mapped .NET claim types, so it
/// works regardless of the handler's inbound claim mapping setting.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var value = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                   ?? user?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        }
    }

    public Guid RequireUserId()
    {
        return UserId ?? throw new UnauthorizedAccessException("User not authenticated");
    }
}

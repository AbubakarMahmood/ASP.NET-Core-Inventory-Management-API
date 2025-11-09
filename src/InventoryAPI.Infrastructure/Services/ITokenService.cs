using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Infrastructure.Services;

/// <summary>
/// JWT token generation service interface
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiryTime();
}

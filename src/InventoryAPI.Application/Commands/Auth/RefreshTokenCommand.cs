using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Exchange a valid refresh token for a new access/refresh token pair
/// </summary>
public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}

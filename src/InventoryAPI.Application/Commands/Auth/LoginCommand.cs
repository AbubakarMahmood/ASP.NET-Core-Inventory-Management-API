using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Login command
/// </summary>
public class LoginCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

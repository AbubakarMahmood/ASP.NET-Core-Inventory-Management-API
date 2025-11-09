using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Commands.Users;

/// <summary>
/// Command to update an existing user
/// </summary>
public class UpdateUserCommand : IRequest<UserDto>
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Command to change user password
/// </summary>
public class ChangePasswordCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Command to delete a user (soft delete)
/// </summary>
public class DeleteUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}

using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.Users;

/// <summary>
/// Handler for updating user information
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (user.Email != normalizedEmail)
        {
            var emailTaken = await _unitOfWork.Users.AnyAsync(
                u => u.Email == normalizedEmail && u.Id != request.UserId, cancellationToken);

            if (emailTaken)
            {
                throw new ValidationException("Email", $"A user with email {request.Email} already exists");
            }

            user.Email = normalizedEmail;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }
}

/// <summary>
/// Handler for changing user password
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public ChangePasswordCommandHandler(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);

        // Invalidate any outstanding refresh token so the password change takes
        // effect immediately.
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

/// <summary>
/// Handler for deleting a user (soft delete)
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        _unitOfWork.Users.Remove(user); // Soft delete via BaseAuditableEntity
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

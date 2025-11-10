using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using MediatR;

namespace InventoryAPI.Application.Commands.Users;

/// <summary>
/// Handler for updating user information
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        // Check if email is changing and if new email already exists
        if (user.Email.ToLower() != request.Email.ToLower())
        {
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(
                u => u.Email.ToLower() == request.Email.ToLower(),
                cancellationToken);

            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email {request.Email} already exists");
            }

            user.Email = request.Email.ToLower();
        }

        // Update user properties
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
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
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        // Hash new password
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);

        // Clear refresh token to force re-login
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
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        _unitOfWork.Users.Remove(user); // This will soft delete due to BaseAuditableEntity
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

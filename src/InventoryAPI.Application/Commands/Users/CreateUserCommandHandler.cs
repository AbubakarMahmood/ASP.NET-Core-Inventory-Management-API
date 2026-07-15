using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.Users;

/// <summary>
/// Handler for creating a new user
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _currentUser = currentUser;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _unitOfWork.Users.AnyAsync(
            u => u.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
        {
            throw new ValidationException("Email", $"A user with email {request.Email} already exists");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Email ?? "System"
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }
}

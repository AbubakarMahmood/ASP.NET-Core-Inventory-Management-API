using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InventoryAPI.Application.Commands.Users;

/// <summary>
/// Handler for creating a new user
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == request.Email.ToLower(),
            cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists");
        }

        // Get current user for audit
        var currentUserEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ?? "System";

        // Hash password
        var passwordHash = _passwordService.HashPassword(request.Password);

        // Create user entity
        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedBy = currentUserEmail
        };

        // Save to database
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var dto = _mapper.Map<UserDto>(user);
        return dto;
    }
}

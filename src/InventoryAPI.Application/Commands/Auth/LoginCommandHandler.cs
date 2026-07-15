using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Exceptions;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Login command handler
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Same error for unknown user and wrong password so the endpoint
        // cannot be used to probe which emails exist.
        if (user == null || !user.IsActive ||
            !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ValidationException("Credentials", "Invalid email or password");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiryTime();

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _tokenService.AccessTokenLifetimeMinutes * 60,
            TokenType = "Bearer"
        };
    }
}

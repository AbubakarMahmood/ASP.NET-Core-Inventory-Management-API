using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Exceptions;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Authenticates a user and rotates the single active refresh-token session.
/// Only a one-way token digest is persisted.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private const string DummyPasswordHash =
        "$pbkdf2-sha256$600000$AAECAwQFBgcICQoLDA0ODw==$s/rS2WRM+ibLOWrMa18JCsxE5ZHI9+Hnl+tR3TNMAsE=";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenHasher _refreshTokenHasher;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService,
        IRefreshTokenHasher refreshTokenHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _refreshTokenHasher = refreshTokenHasher;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        var passwordHash = user is { IsActive: true }
            ? user.PasswordHash
            : DummyPasswordHash;
        var passwordIsValid = _passwordService.VerifyPassword(request.Password, passwordHash);

        if (user == null || !user.IsActive || !passwordIsValid)
        {
            throw new ValidationException("Credentials", "Invalid email or password");
        }

        if (_passwordService.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordService.HashPassword(request.Password);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
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

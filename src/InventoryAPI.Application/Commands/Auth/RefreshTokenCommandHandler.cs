using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Refresh token command handler. Rotates the refresh token on every use.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || !user.IsActive ||
            user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
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

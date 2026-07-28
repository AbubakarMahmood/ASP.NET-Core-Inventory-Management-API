using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Rotates the refresh token on every successful use. Reuse of the prior token
/// fails because its persisted digest is replaced atomically.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenHasher _refreshTokenHasher;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IRefreshTokenHasher refreshTokenHasher)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _refreshTokenHasher = refreshTokenHasher;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenHasher.Hash(request.RefreshToken);
        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == tokenHash, cancellationToken);

        if (user == null || !user.IsActive ||
            user.RefreshTokenHash == null ||
            !_refreshTokenHasher.Verify(request.RefreshToken, user.RefreshTokenHash) ||
            user.RefreshTokenExpiryTime == null ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
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

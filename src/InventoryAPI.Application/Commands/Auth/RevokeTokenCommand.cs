using InventoryAPI.Application.Interfaces;
using MediatR;

namespace InventoryAPI.Application.Commands.Auth;

/// <summary>
/// Revoke the current user's refresh token (logout)
/// </summary>
public class RevokeTokenCommand : IRequest<Unit>
{
}

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RevokeTokenCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}

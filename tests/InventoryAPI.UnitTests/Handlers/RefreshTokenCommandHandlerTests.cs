using System.Linq.Expressions;
using FluentAssertions;
using InventoryAPI.Application.Commands.Auth;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using Moq;

namespace InventoryAPI.UnitTests.Handlers;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("new-access");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");
        _tokenService.Setup(t => t.GetRefreshTokenExpiryTime()).Returns(DateTime.UtcNow.AddDays(7));
        _tokenService.SetupGet(t => t.AccessTokenLifetimeMinutes).Returns(60);

        _handler = new RefreshTokenCommandHandler(_unitOfWork.Object, _tokenService.Object);
    }

    private void SetupUser(User? user) =>
        _users.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_ValidToken_RotatesRefreshToken()
    {
        var user = new User
        {
            IsActive = true,
            RefreshToken = "old-refresh",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        SetupUser(user);

        var result = await _handler.Handle(new RefreshTokenCommand { RefreshToken = "old-refresh" }, default);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        user.RefreshToken.Should().Be("new-refresh", "refresh tokens must rotate on use");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        SetupUser(new User
        {
            IsActive = true,
            RefreshToken = "old-refresh",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1)
        });

        var act = () => _handler.Handle(new RefreshTokenCommand { RefreshToken = "old-refresh" }, default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsUnauthorized()
    {
        SetupUser(null);

        var act = () => _handler.Handle(new RefreshTokenCommand { RefreshToken = "does-not-exist" }, default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorized()
    {
        SetupUser(new User
        {
            IsActive = false,
            RefreshToken = "old-refresh",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        });

        var act = () => _handler.Handle(new RefreshTokenCommand { RefreshToken = "old-refresh" }, default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

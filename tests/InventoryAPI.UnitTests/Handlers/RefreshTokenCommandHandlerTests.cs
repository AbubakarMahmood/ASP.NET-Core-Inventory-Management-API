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
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IRefreshTokenHasher> _hashes = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unit => unit.Users).Returns(_users.Object);
        _tokens.Setup(service => service.GenerateAccessToken(It.IsAny<User>())).Returns("new-access");
        _tokens.Setup(service => service.GenerateRefreshToken()).Returns("new-refresh");
        _tokens.Setup(service => service.GetRefreshTokenExpiryTime()).Returns(DateTime.UtcNow.AddDays(7));
        _tokens.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(60);
        _hashes.Setup(service => service.Hash("old-refresh")).Returns("OLD-HASH");
        _hashes.Setup(service => service.Hash("new-refresh")).Returns("NEW-HASH");
        _hashes.Setup(service => service.Hash("unknown")).Returns("UNKNOWN-HASH");
        _hashes.Setup(service => service.Verify("old-refresh", "OLD-HASH")).Returns(true);
        _handler = new RefreshTokenCommandHandler(_unitOfWork.Object, _tokens.Object, _hashes.Object);
    }

    private void UserResult(User? user) =>
        _users.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_ValidToken_RotatesAndStoresNewHash()
    {
        var user = new User
        {
            IsActive = true,
            RefreshTokenHash = "OLD-HASH",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        UserResult(user);

        var result = await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "old-refresh" },
            TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        user.RefreshTokenHash.Should().Be("NEW-HASH");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        UserResult(new User
        {
            IsActive = true,
            RefreshTokenHash = "OLD-HASH",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddSeconds(-1)
        });
        var act = () => _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "old-refresh" },
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsUnauthorized()
    {
        UserResult(null);
        var act = () => _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "unknown" },
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_HashVerificationFailure_ThrowsUnauthorized()
    {
        UserResult(new User
        {
            IsActive = true,
            RefreshTokenHash = "OLD-HASH",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        });
        _hashes.Setup(service => service.Verify("old-refresh", "OLD-HASH")).Returns(false);
        var act = () => _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "old-refresh" },
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

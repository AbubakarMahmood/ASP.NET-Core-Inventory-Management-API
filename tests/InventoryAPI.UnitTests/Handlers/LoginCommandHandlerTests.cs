using System.Linq.Expressions;
using FluentAssertions;
using InventoryAPI.Application.Commands.Auth;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using Moq;

namespace InventoryAPI.UnitTests.Handlers;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IPasswordService> _passwords = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IRefreshTokenHasher> _refreshHashes = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unit => unit.Users).Returns(_users.Object);
        _tokens.Setup(service => service.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _tokens.Setup(service => service.GenerateRefreshToken()).Returns("refresh-token");
        _tokens.Setup(service => service.GetRefreshTokenExpiryTime()).Returns(DateTime.UtcNow.AddDays(7));
        _tokens.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(60);
        _refreshHashes.Setup(service => service.Hash("refresh-token")).Returns("REFRESH-HASH");
        _handler = new LoginCommandHandler(
            _unitOfWork.Object, _passwords.Object, _tokens.Object, _refreshHashes.Object);
    }

    private void UserResult(User? user) =>
        _users.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = "hash",
        IsActive = true
    };

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsRawTokenAndStoresOnlyHash()
    {
        var user = ActiveUser();
        UserResult(user);
        _passwords.Setup(service => service.VerifyPassword("Password1", "hash")).Returns(true);
        _passwords.Setup(service => service.NeedsRehash("hash")).Returns(false);

        var result = await _handler.Handle(
            new LoginCommand { Email = " USER@EXAMPLE.COM ", Password = "Password1" },
            TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(3600);
        user.RefreshTokenHash.Should().Be("REFRESH-HASH");
        user.RefreshTokenHash.Should().NotBe(result.RefreshToken);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LegacyPasswordHash_UpgradesAfterVerification()
    {
        var user = ActiveUser();
        UserResult(user);
        _passwords.Setup(service => service.VerifyPassword("Password1", "hash")).Returns(true);
        _passwords.Setup(service => service.NeedsRehash("hash")).Returns(true);
        _passwords.Setup(service => service.HashPassword("Password1")).Returns("versioned-hash");

        await _handler.Handle(
            new LoginCommand { Email = user.Email, Password = "Password1" },
            TestContext.Current.CancellationToken);

        user.PasswordHash.Should().Be("versioned-hash");
    }

    [Fact]
    public async Task Handle_UnknownEmail_UsesGenericCredentialError()
    {
        UserResult(null);
        var act = () => _handler.Handle(
            new LoginCommand { Email = "nobody@example.com", Password = "Password1" },
            TestContext.Current.CancellationToken);
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Values.SelectMany(values => values)
            .Should().Contain("Invalid email or password");
        _passwords.Verify(service => service.VerifyPassword(
            "Password1",
            It.Is<string>(hash => hash.StartsWith("$pbkdf2-sha256$600000$", StringComparison.Ordinal))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WrongPassword_UsesGenericCredentialError()
    {
        UserResult(ActiveUser());
        _passwords.Setup(service => service.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        var act = () => _handler.Handle(
            new LoginCommand { Email = "user@example.com", Password = "wrong" },
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_DoesNotIssueTokens()
    {
        var user = ActiveUser();
        user.IsActive = false;
        UserResult(user);
        var act = () => _handler.Handle(
            new LoginCommand { Email = user.Email, Password = "Password1" },
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ValidationException>();
        _passwords.Verify(service => service.VerifyPassword(
            "Password1",
            It.Is<string>(hash => hash.StartsWith("$pbkdf2-sha256$600000$", StringComparison.Ordinal))),
            Times.Once);
        _tokens.Verify(service => service.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }
}

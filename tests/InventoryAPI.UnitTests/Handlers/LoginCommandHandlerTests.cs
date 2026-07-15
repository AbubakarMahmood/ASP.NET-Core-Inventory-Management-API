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
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        _tokenService.Setup(t => t.GetRefreshTokenExpiryTime()).Returns(DateTime.UtcNow.AddDays(7));
        _tokenService.SetupGet(t => t.AccessTokenLifetimeMinutes).Returns(60);

        _handler = new LoginCommandHandler(_unitOfWork.Object, _passwordService.Object, _tokenService.Object);
    }

    private void SetupUser(User? user) =>
        _users.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = "hash",
        IsActive = true
    };

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokensAndStoresRefreshToken()
    {
        var user = ActiveUser();
        SetupUser(user);
        _passwordService.Setup(p => p.VerifyPassword("Password1", "hash")).Returns(true);

        var result = await _handler.Handle(
            new LoginCommand { Email = "user@example.com", Password = "Password1" }, default);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(3600);
        user.RefreshToken.Should().Be("refresh-token");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsSameErrorAsWrongPassword()
    {
        SetupUser(null);

        var act = () => _handler.Handle(
            new LoginCommand { Email = "nobody@example.com", Password = "Password1" }, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Values.SelectMany(v => v)
            .Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WrongPassword_Throws()
    {
        SetupUser(ActiveUser());
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = () => _handler.Handle(
            new LoginCommand { Email = "user@example.com", Password = "wrong" }, default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_Throws()
    {
        var user = ActiveUser();
        user.IsActive = false;
        SetupUser(user);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var act = () => _handler.Handle(
            new LoginCommand { Email = "user@example.com", Password = "Password1" }, default);

        await act.Should().ThrowAsync<ValidationException>();
    }
}

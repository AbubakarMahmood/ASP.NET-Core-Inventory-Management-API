using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace InventoryAPI.UnitTests.Services;

public class TokenServiceTests
{
    private static TokenService Service(int expiryMinutes = 60) => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "unit-test-signing-key-that-is-long-enough-123456",
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:ExpiryMinutes"] = expiryMinutes.ToString(),
            ["JwtSettings:RefreshTokenExpiryDays"] = "7"
        }).Build());

    private static User User() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        FirstName = "Test",
        LastName = "User",
        Role = UserRole.Manager
    };

    [Fact]
    public void GenerateAccessToken_ContainsIdentityAndRoleClaims()
    {
        var user = User();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(Service().GenerateAccessToken(user));
        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
        jwt.Claims.Should().Contain(claim => claim.Type == "sub" && claim.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == "email" && claim.Value == user.Email);
        jwt.Claims.Should().Contain(claim => claim.Value == "Manager");
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredExpiry()
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(Service(30).GenerateAccessToken(User()));
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueHighEntropyValues()
    {
        var values = Enumerable.Range(0, 20).Select(_ => Service().GenerateRefreshToken()).ToList();
        values.Should().OnlyHaveUniqueItems();
        values.Should().OnlyContain(value => Convert.FromBase64String(value).Length == 64);
    }

    [Fact]
    public void GetRefreshTokenExpiry_UsesConfiguredDays()
    {
        Service().GetRefreshTokenExpiryTime()
            .Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }
}

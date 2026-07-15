using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace InventoryAPI.UnitTests.Services;

public class TokenServiceTests
{
    private static TokenService CreateService(int expiryMinutes = 60)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "unit-test-signing-key-that-is-long-enough-123456",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpiryMinutes"] = expiryMinutes.ToString(),
                ["JwtSettings:RefreshTokenExpiryDays"] = "7"
            })
            .Build();

        return new TokenService(configuration);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        FirstName = "Test",
        LastName = "User",
        Role = UserRole.Manager
    };

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var user = CreateUser();
        var token = CreateService().GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Value == "Manager");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresPerConfiguration()
    {
        var token = CreateService(expiryMinutes: 30).GenerateAccessToken(CreateUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueValues()
    {
        var service = CreateService();

        var tokens = Enumerable.Range(0, 20).Select(_ => service.GenerateRefreshToken()).ToList();

        tokens.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetRefreshTokenExpiryTime_UsesConfiguredDays()
    {
        var expiry = CreateService().GetRefreshTokenExpiryTime();

        expiry.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }
}

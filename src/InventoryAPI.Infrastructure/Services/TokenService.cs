using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InventoryAPI.Infrastructure.Services;

/// <summary>
/// JWT token generation service
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int AccessTokenLifetimeMinutes =>
        int.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var minutes) ? minutes : 60;

    public string GenerateAccessToken(User user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public DateTime GetRefreshTokenExpiryTime()
    {
        var expiryDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpiryDays"], out var days) ? days : 7;
        return DateTime.UtcNow.AddDays(expiryDays);
    }
}

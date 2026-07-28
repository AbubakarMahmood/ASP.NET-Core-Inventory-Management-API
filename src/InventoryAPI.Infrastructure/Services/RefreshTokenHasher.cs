using System.Security.Cryptography;
using System.Text;
using InventoryAPI.Application.Interfaces;

namespace InventoryAPI.Infrastructure.Services;

/// <summary>
/// SHA-256 is appropriate here because refresh tokens are generated from 512
/// bits of cryptographic randomness. This is not a password hashing primitive.
/// </summary>
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public bool Verify(string token, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var actualBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}

using System.Globalization;
using System.Security.Cryptography;
using InventoryAPI.Application.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace InventoryAPI.Infrastructure.Services;

/// <summary>
/// Versioned PBKDF2 password hashing with support for transparently upgrading
/// the repository's original salt-plus-hash Base64 representation.
/// </summary>
public class PasswordService : IPasswordService
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int CurrentIterations = 600_000;
    private const int LegacyIterations = 100_000;
    private const int MaximumAcceptedIterations = 2_000_000;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, CurrentIterations);

        return string.Join(
            '$',
            Algorithm,
            CurrentIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            if (TryParseVersionedHash(passwordHash, out var iterations, out var salt, out var expectedHash))
            {
                var actualHash = Derive(password, salt, iterations);
                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }

            return VerifyLegacyHash(password, passwordHash);
        }
        catch (Exception exception) when (
            exception is FormatException
            or ArgumentException
            or OverflowException)
        {
            return false;
        }
    }

    public bool NeedsRehash(string passwordHash)
    {
        try
        {
            return !TryParseVersionedHash(passwordHash, out var iterations, out _, out _)
                || iterations != CurrentIterations;
        }
        catch (Exception exception) when (
            exception is FormatException
            or ArgumentException
            or OverflowException)
        {
            return true;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
    {
        return KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: HashSize);
    }

    private static bool VerifyLegacyHash(string password, string passwordHash)
    {
        var bytes = Convert.FromBase64String(passwordHash);
        if (bytes.Length != SaltSize + HashSize)
        {
            return false;
        }

        var salt = bytes.AsSpan(0, SaltSize).ToArray();
        var expectedHash = bytes.AsSpan(SaltSize, HashSize);
        var actualHash = Derive(password, salt, LegacyIterations);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static bool TryParseVersionedHash(
        string passwordHash,
        out int iterations,
        out byte[] salt,
        out byte[] expectedHash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        expectedHash = Array.Empty<byte>();

        var parts = passwordHash.Split('$');
        if (parts.Length != 4
            || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations <= 0
            || iterations > MaximumAcceptedIterations)
        {
            return false;
        }

        salt = Convert.FromBase64String(parts[2]);
        expectedHash = Convert.FromBase64String(parts[3]);

        return salt.Length == SaltSize && expectedHash.Length == HashSize;
    }
}

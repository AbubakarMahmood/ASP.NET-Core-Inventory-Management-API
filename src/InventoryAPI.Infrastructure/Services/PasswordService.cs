using System.Security.Cryptography;
using InventoryAPI.Application.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace InventoryAPI.Infrastructure.Services;

/// <summary>
/// Password hashing and verification service using PBKDF2
/// </summary>
public class PasswordService : IPasswordService
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        // Generate a random salt
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash the password
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        // Combine salt and hash
        byte[] hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            // Extract the bytes
            byte[] hashBytes = Convert.FromBase64String(passwordHash);

            // Get the salt
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Compute the hash on the password the user entered
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize);

            // Constant-time comparison to avoid leaking timing information
            return CryptographicOperations.FixedTimeEquals(
                hashBytes.AsSpan(SaltSize, HashSize),
                hash);
        }
        catch (FormatException)
        {
            // Stored value is not valid Base64
            return false;
        }
        catch (ArgumentException)
        {
            // Stored value is shorter than salt + hash
            return false;
        }
    }
}

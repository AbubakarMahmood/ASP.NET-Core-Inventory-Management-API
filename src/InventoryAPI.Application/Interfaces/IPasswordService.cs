namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Password hashing, verification, and transparent upgrade service.
/// </summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Returns true when a successfully verified stored hash uses a legacy or
    /// weaker format and should be replaced during login.
    /// </summary>
    bool NeedsRehash(string passwordHash);
}

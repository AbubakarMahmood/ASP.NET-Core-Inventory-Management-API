namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Password hashing and verification service interface
/// </summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

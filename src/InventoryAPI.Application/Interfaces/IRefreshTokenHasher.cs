namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Converts high-entropy refresh tokens into a one-way representation before
/// persistence. Raw refresh tokens are returned to clients once and are never
/// stored by the server.
/// </summary>
public interface IRefreshTokenHasher
{
    string Hash(string token);
    bool Verify(string token, string expectedHash);
}

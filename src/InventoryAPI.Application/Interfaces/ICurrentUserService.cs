namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Provides information about the user executing the current request
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's id, or null when the request is anonymous
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The authenticated user's email, or null when the request is anonymous
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The authenticated user's id. Throws <see cref="UnauthorizedAccessException"/> when anonymous.
    /// </summary>
    Guid RequireUserId();
}

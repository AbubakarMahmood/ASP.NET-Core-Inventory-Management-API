using InventoryAPI.BlazorUI.Models;
using System.Text;

namespace InventoryAPI.BlazorUI.Services;

public class UserService
{
    private readonly ApiClient _apiClient;

    public UserService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Get users with optional filtering
    /// </summary>
    public async Task<PaginatedResult<UserDto>?> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        UserRole? role = null,
        bool? isActive = null,
        string? searchTerm = null)
    {
        var queryParams = new StringBuilder();
        queryParams.Append($"pageNumber={pageNumber}&pageSize={pageSize}");

        if (role.HasValue)
            queryParams.Append($"&role={role.Value}");

        if (isActive.HasValue)
            queryParams.Append($"&isActive={isActive.Value}");

        if (!string.IsNullOrWhiteSpace(searchTerm))
            queryParams.Append($"&searchTerm={Uri.EscapeDataString(searchTerm)}");

        return await _apiClient.GetAsync<PaginatedResult<UserDto>>(
            $"/api/v1/users?{queryParams}");
    }

    /// <summary>
    /// Get a single user by ID
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<UserDto>($"/api/v1/users/{id}");
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request)
    {
        return await _apiClient.PostAsync<CreateUserRequest, UserDto>(
            "/api/v1/users",
            request);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        return await _apiClient.PutAsync<UpdateUserRequest, UserDto>(
            $"/api/v1/users/{id}",
            request);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(Guid id, string newPassword)
    {
        var request = new ChangePasswordRequest { NewPassword = newPassword };
        var response = await _apiClient.PostAsync<ChangePasswordRequest, object>(
            $"/api/v1/users/{id}/change-password",
            request);
        return response != null;
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var response = await _apiClient.DeleteAsync($"/api/v1/users/{id}");
        return response.IsSuccessStatusCode;
    }
}

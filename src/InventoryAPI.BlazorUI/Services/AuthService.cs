using Blazored.LocalStorage;
using InventoryAPI.BlazorUI.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace InventoryAPI.BlazorUI.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (authResponse != null)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.AccessToken);
                    await _localStorage.SetItemAsync("refreshToken", authResponse.RefreshToken);

                    ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.AccessToken);

                    return authResponse;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");

        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        return !string.IsNullOrEmpty(token);
    }
}

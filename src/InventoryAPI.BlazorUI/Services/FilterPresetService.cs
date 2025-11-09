using InventoryAPI.BlazorUI.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace InventoryAPI.BlazorUI.Services;

/// <summary>
/// Service for managing filter presets
/// </summary>
public class FilterPresetService
{
    private readonly ApiClient _apiClient;

    public FilterPresetService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<FilterPresetDto>> GetFilterPresetsAsync(string? entityType = null, bool includeShared = true)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(entityType))
        {
            queryParams.Add($"entityType={entityType}");
        }

        queryParams.Add($"includeShared={includeShared.ToString().ToLower()}");

        var query = queryParams.Any() ? $"?{string.Join("&", queryParams)}" : "";
        var response = await _apiClient.HttpClient.GetAsync($"api/v1/filterpresets{query}");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<FilterPresetDto>>() ?? new List<FilterPresetDto>();
        }

        return new List<FilterPresetDto>();
    }

    public async Task<FilterPresetDto?> GetFilterPresetByIdAsync(Guid id)
    {
        var response = await _apiClient.HttpClient.GetAsync($"api/v1/filterpresets/{id}");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FilterPresetDto>();
        }

        return null;
    }

    public async Task<FilterPresetDto?> CreateFilterPresetAsync(string name, string entityType, string filterData, bool isDefault = false, bool isShared = false)
    {
        var command = new
        {
            name,
            entityType,
            filterData,
            isDefault,
            isShared
        };

        var response = await _apiClient.HttpClient.PostAsJsonAsync("api/v1/filterpresets", command);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FilterPresetDto>();
        }

        return null;
    }

    public async Task<FilterPresetDto?> UpdateFilterPresetAsync(Guid id, string name, string filterData, bool isDefault = false, bool isShared = false)
    {
        var command = new
        {
            id,
            name,
            filterData,
            isDefault,
            isShared
        };

        var response = await _apiClient.HttpClient.PutAsJsonAsync($"api/v1/filterpresets/{id}", command);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FilterPresetDto>();
        }

        return null;
    }

    public async Task<bool> DeleteFilterPresetAsync(Guid id)
    {
        var response = await _apiClient.HttpClient.DeleteAsync($"api/v1/filterpresets/{id}");
        return response.IsSuccessStatusCode;
    }
}

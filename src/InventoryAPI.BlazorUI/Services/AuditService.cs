using InventoryAPI.BlazorUI.Models;
using System.Net.Http.Json;

namespace InventoryAPI.BlazorUI.Services;

/// <summary>
/// Service for managing audit logs
/// </summary>
public class AuditService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditService> _logger;

    public AuditService(HttpClient httpClient, ILogger<AuditService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with optional filtering
    /// </summary>
    public async Task<PaginatedResult<AuditLogDto>?> GetAuditLogsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? entityType = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? performedBy = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(entityType))
                queryParams.Add($"entityType={Uri.EscapeDataString(entityType)}");

            if (!string.IsNullOrWhiteSpace(action))
                queryParams.Add($"action={Uri.EscapeDataString(action)}");

            if (fromDate.HasValue)
                queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("yyyy-MM-dd"))}");

            if (toDate.HasValue)
                queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("yyyy-MM-dd"))}");

            if (!string.IsNullOrWhiteSpace(performedBy))
                queryParams.Add($"performedBy={Uri.EscapeDataString(performedBy)}");

            var query = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"api/v1/audit?{query}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLogDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs");
            throw;
        }
    }
}

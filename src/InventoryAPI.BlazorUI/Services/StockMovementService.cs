using InventoryAPI.BlazorUI.Models;

namespace InventoryAPI.BlazorUI.Services;

public class StockMovementService
{
    private readonly ApiClient _apiClient;

    public StockMovementService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Placeholder methods - to be implemented
    public async Task<PaginatedResult<StockMovementDto>?> GetStockMovementsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        Guid? productId = null,
        StockMovementType? type = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = $"pageNumber={pageNumber}&pageSize={pageSize}";

        if (productId.HasValue)
        {
            query += $"&productId={productId.Value}";
        }

        if (type.HasValue)
        {
            query += $"&type={type.Value}";
        }

        if (fromDate.HasValue)
        {
            query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
        }

        if (toDate.HasValue)
        {
            query += $"&toDate={toDate.Value:yyyy-MM-dd}";
        }

        return await _apiClient.GetAsync<PaginatedResult<StockMovementDto>>($"/api/v1/stock-movements?{query}");
    }

    public async Task<StockMovementDto?> RecordStockMovementAsync(RecordStockMovementRequest request)
    {
        return await _apiClient.PostAsync<RecordStockMovementRequest, StockMovementDto>("/api/v1/stock-movements", request);
    }

    public async Task<StockMovementStatistics?> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = "";

        if (fromDate.HasValue)
        {
            query += $"fromDate={fromDate.Value:yyyy-MM-dd}";
        }

        if (toDate.HasValue)
        {
            if (!string.IsNullOrEmpty(query)) query += "&";
            query += $"toDate={toDate.Value:yyyy-MM-dd}";
        }

        var endpoint = "/api/v1/stock-movements/statistics";
        if (!string.IsNullOrEmpty(query))
        {
            endpoint += $"?{query}";
        }

        return await _apiClient.GetAsync<StockMovementStatistics>(endpoint);
    }
}

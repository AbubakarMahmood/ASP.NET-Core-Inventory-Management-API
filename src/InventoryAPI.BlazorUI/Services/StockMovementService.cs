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
    public async Task<PaginatedResult<StockMovementDto>?> GetStockMovementsAsync(int pageNumber = 1, int pageSize = 10, Guid? productId = null)
    {
        var query = $"pageNumber={pageNumber}&pageSize={pageSize}";
        if (productId.HasValue)
        {
            query += $"&productId={productId.Value}";
        }
        return await _apiClient.GetAsync<PaginatedResult<StockMovementDto>>($"/api/v1/stock-movements?{query}");
    }

    public async Task<StockMovementDto?> RecordStockMovementAsync(RecordStockMovementRequest request)
    {
        return await _apiClient.PostAsync<RecordStockMovementRequest, StockMovementDto>("/api/v1/stock-movements", request);
    }

    public async Task<StockMovementStatistics?> GetStatisticsAsync()
    {
        return await _apiClient.GetAsync<StockMovementStatistics>("/api/v1/stock-movements/statistics");
    }
}

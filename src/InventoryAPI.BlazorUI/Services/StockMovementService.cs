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
    public async Task<PaginatedResult<StockMovementDto>?> GetStockMovementsAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _apiClient.GetAsync<PaginatedResult<StockMovementDto>>($"/api/v1/stock-movements?pageNumber={pageNumber}&pageSize={pageSize}");
    }
}

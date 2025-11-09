using InventoryAPI.BlazorUI.Models;
using System.Text;

namespace InventoryAPI.BlazorUI.Services;

public class StockMovementService
{
    private readonly ApiClient _apiClient;

    public StockMovementService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Get stock movements with optional filtering
    /// </summary>
    public async Task<PaginatedResult<StockMovementDto>?> GetStockMovementsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? productId = null,
        StockMovementType? type = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var queryParams = new StringBuilder();
        queryParams.Append($"pageNumber={pageNumber}&pageSize={pageSize}");

        if (productId.HasValue)
            queryParams.Append($"&productId={productId.Value}");

        if (type.HasValue)
            queryParams.Append($"&type={type.Value}");

        if (fromDate.HasValue)
            queryParams.Append($"&fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}");

        if (toDate.HasValue)
            queryParams.Append($"&toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}");

        return await _apiClient.GetAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements?{queryParams}");
    }

    /// <summary>
    /// Get stock movements for a specific product
    /// </summary>
    public async Task<PaginatedResult<StockMovementDto>?> GetProductStockMovementsAsync(
        Guid productId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        return await _apiClient.GetAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements/product/{productId}?pageNumber={pageNumber}&pageSize={pageSize}");
    }

    /// <summary>
    /// Record a new stock movement
    /// </summary>
    public async Task<StockMovementDto?> RecordStockMovementAsync(RecordStockMovementRequest request)
    {
        return await _apiClient.PostAsync<RecordStockMovementRequest, StockMovementDto>(
            "/api/v1/stockmovements",
            request);
    }

    /// <summary>
    /// Get stock movement statistics for a date range
    /// </summary>
    public async Task<StockMovementStatistics?> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var queryParams = new StringBuilder();
        var hasParams = false;

        if (fromDate.HasValue)
        {
            queryParams.Append($"fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}");
            hasParams = true;
        }

        if (toDate.HasValue)
        {
            if (hasParams) queryParams.Append("&");
            queryParams.Append($"toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}");
        }

        var url = hasParams
            ? $"/api/v1/stockmovements/statistics?{queryParams}"
            : "/api/v1/stockmovements/statistics";

        return await _apiClient.GetAsync<StockMovementStatistics>(url);
    }
}

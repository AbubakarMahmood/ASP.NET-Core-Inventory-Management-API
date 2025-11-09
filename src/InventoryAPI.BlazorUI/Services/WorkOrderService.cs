using InventoryAPI.BlazorUI.Models;

namespace InventoryAPI.BlazorUI.Services;

public class WorkOrderService
{
    private readonly ApiClient _apiClient;

    public WorkOrderService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Placeholder methods - to be implemented
    public async Task<PaginatedResult<WorkOrderDto>?> GetWorkOrdersAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _apiClient.GetAsync<PaginatedResult<WorkOrderDto>>($"/api/v1/work-orders?pageNumber={pageNumber}&pageSize={pageSize}");
    }
}

public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

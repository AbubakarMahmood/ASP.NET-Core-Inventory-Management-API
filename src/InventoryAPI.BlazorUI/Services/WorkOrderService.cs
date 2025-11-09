using InventoryAPI.BlazorUI.Models;
using System.Text;

namespace InventoryAPI.BlazorUI.Services;

public class WorkOrderService
{
    private readonly ApiClient _apiClient;

    public WorkOrderService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Get work orders with optional filtering
    /// </summary>
    public async Task<PaginatedResult<WorkOrderDto>?> GetWorkOrdersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        WorkOrderStatus? status = null,
        WorkOrderPriority? priority = null,
        Guid? assignedToId = null,
        Guid? requestedById = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var queryParams = new StringBuilder();
        queryParams.Append($"pageNumber={pageNumber}&pageSize={pageSize}");

        if (status.HasValue)
            queryParams.Append($"&status={status.Value}");

        if (priority.HasValue)
            queryParams.Append($"&priority={priority.Value}");

        if (assignedToId.HasValue)
            queryParams.Append($"&assignedToId={assignedToId.Value}");

        if (requestedById.HasValue)
            queryParams.Append($"&requestedById={requestedById.Value}");

        if (fromDate.HasValue)
            queryParams.Append($"&fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}");

        if (toDate.HasValue)
            queryParams.Append($"&toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}");

        return await _apiClient.GetAsync<PaginatedResult<WorkOrderDto>>(
            $"/api/v1/workorders?{queryParams}");
    }

    /// <summary>
    /// Get a single work order by ID
    /// </summary>
    public async Task<WorkOrderDto?> GetWorkOrderByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<WorkOrderDto>($"/api/v1/workorders/{id}");
    }

    /// <summary>
    /// Create a new work order
    /// </summary>
    public async Task<WorkOrderDto?> CreateWorkOrderAsync(CreateWorkOrderRequest request)
    {
        return await _apiClient.PostAsync<CreateWorkOrderRequest, WorkOrderDto>(
            "/api/v1/workorders",
            request);
    }

    /// <summary>
    /// Submit a draft work order
    /// </summary>
    public async Task<WorkOrderDto?> SubmitWorkOrderAsync(Guid id)
    {
        return await _apiClient.PostAsync<object, WorkOrderDto>(
            $"/api/v1/workorders/{id}/submit",
            new { });
    }

    /// <summary>
    /// Approve a submitted work order
    /// </summary>
    public async Task<WorkOrderDto?> ApproveWorkOrderAsync(Guid id, Guid assignedToId)
    {
        return await _apiClient.PostAsync<ApproveWorkOrderRequest, WorkOrderDto>(
            $"/api/v1/workorders/{id}/approve",
            new ApproveWorkOrderRequest { AssignedToId = assignedToId });
    }

    /// <summary>
    /// Reject a submitted work order
    /// </summary>
    public async Task<WorkOrderDto?> RejectWorkOrderAsync(Guid id, string reason)
    {
        return await _apiClient.PostAsync<RejectWorkOrderRequest, WorkOrderDto>(
            $"/api/v1/workorders/{id}/reject",
            new RejectWorkOrderRequest { Reason = reason });
    }

    /// <summary>
    /// Start an approved work order
    /// </summary>
    public async Task<WorkOrderDto?> StartWorkOrderAsync(Guid id)
    {
        return await _apiClient.PostAsync<object, WorkOrderDto>(
            $"/api/v1/workorders/{id}/start",
            new { });
    }

    /// <summary>
    /// Complete an in-progress work order
    /// </summary>
    public async Task<WorkOrderDto?> CompleteWorkOrderAsync(Guid id)
    {
        return await _apiClient.PostAsync<object, WorkOrderDto>(
            $"/api/v1/workorders/{id}/complete",
            new { });
    }

    /// <summary>
    /// Cancel a work order
    /// </summary>
    public async Task<WorkOrderDto?> CancelWorkOrderAsync(Guid id)
    {
        return await _apiClient.PostAsync<object, WorkOrderDto>(
            $"/api/v1/workorders/{id}/cancel",
            new { });
    }

    /// <summary>
    /// Issue items from a work order
    /// </summary>
    public async Task<WorkOrderDto?> IssueItemsAsync(Guid id, IssueWorkOrderItemsRequest request)
    {
        return await _apiClient.PostAsync<IssueWorkOrderItemsRequest, WorkOrderDto>(
            $"/api/v1/workorders/{id}/issue-items",
            request);
    }
}

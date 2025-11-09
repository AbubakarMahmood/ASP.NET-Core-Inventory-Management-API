namespace InventoryAPI.BlazorUI.Models;

public class CreateWorkOrderRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public DateTime? DueDate { get; set; }
    public List<CreateWorkOrderItemRequest> Items { get; set; } = new();
}

public class CreateWorkOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int QuantityRequested { get; set; }
    public string? Notes { get; set; }
}

public class ApproveWorkOrderRequest
{
    public Guid AssignedToId { get; set; }
}

public class RejectWorkOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class IssueWorkOrderItemsRequest
{
    public List<IssueItemRequest> Items { get; set; } = new();
}

public class IssueItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? FromLocation { get; set; }
    public string? Notes { get; set; }
}

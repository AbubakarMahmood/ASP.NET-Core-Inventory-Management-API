namespace InventoryAPI.BlazorUI.Models;

public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public WorkOrderPriority Priority { get; set; }
    public WorkOrderStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // User information
    public Guid RequestedById { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string RequestedByEmail { get; set; } = string.Empty;
    public Guid? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public string? AssignedToEmail { get; set; }

    // Items
    public List<WorkOrderItemDto> Items { get; set; } = new();

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class WorkOrderItemDto
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int QuantityRequested { get; set; }
    public int QuantityIssued { get; set; }
    public string? Notes { get; set; }
}

public enum WorkOrderStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7
}

public enum WorkOrderPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

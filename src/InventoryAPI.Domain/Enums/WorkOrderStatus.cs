namespace InventoryAPI.Domain.Enums;

/// <summary>
/// Work order status workflow
/// </summary>
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

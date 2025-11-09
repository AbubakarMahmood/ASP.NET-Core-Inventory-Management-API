using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Work Order entity with workflow management
/// </summary>
public class WorkOrder : BaseAuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Foreign keys
    public Guid RequestedById { get; set; }
    public Guid? AssignedToId { get; set; }

    // Navigation properties
    public User RequestedBy { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public ICollection<WorkOrderItem> Items { get; set; } = new List<WorkOrderItem>();

    // Business logic - State transitions
    public void Submit()
    {
        if (Status != WorkOrderStatus.Draft)
            throw new BusinessRuleViolationException("Only draft work orders can be submitted.");

        if (!Items.Any())
            throw new BusinessRuleViolationException("Cannot submit work order without items.");

        Status = WorkOrderStatus.Submitted;
    }

    public void Approve(Guid assignedToId)
    {
        if (Status != WorkOrderStatus.Submitted)
            throw new BusinessRuleViolationException("Only submitted work orders can be approved.");

        Status = WorkOrderStatus.Approved;
        AssignedToId = assignedToId;
    }

    public void Reject(string reason)
    {
        if (Status != WorkOrderStatus.Submitted)
            throw new BusinessRuleViolationException("Only submitted work orders can be rejected.");

        Status = WorkOrderStatus.Rejected;
    }

    public void Start()
    {
        if (Status != WorkOrderStatus.Approved)
            throw new BusinessRuleViolationException("Only approved work orders can be started.");

        Status = WorkOrderStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != WorkOrderStatus.InProgress)
            throw new BusinessRuleViolationException("Only in-progress work orders can be completed.");

        Status = WorkOrderStatus.Completed;
        CompletedDate = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == WorkOrderStatus.Completed)
            throw new BusinessRuleViolationException("Cannot cancel completed work orders.");

        Status = WorkOrderStatus.Cancelled;
    }
}

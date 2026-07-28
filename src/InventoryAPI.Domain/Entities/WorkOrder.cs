using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// Work order with an explicit approval, fulfilment, and completion workflow.
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
    public string? RejectionReason { get; set; }

    public Guid RequestedById { get; set; }
    public Guid? AssignedToId { get; set; }

    public User RequestedBy { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public ICollection<WorkOrderItem> Items { get; set; } = new List<WorkOrderItem>();

    public bool IsFullyIssued => Items.Count > 0 && Items.All(item => item.IsFullyIssued);

    public void Submit()
    {
        if (Status != WorkOrderStatus.Draft)
            throw new BusinessRuleViolationException("Only draft work orders can be submitted.");

        if (Items.Count == 0)
            throw new BusinessRuleViolationException("Cannot submit a work order without items.");

        if (Items.Any(item => item.QuantityRequested <= 0))
            throw new BusinessRuleViolationException("Every work order item must request a positive quantity.");

        if (Items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            throw new BusinessRuleViolationException("A product may appear only once on a work order.");

        Status = WorkOrderStatus.Submitted;
    }

    public void Approve(Guid assignedToId)
    {
        if (Status != WorkOrderStatus.Submitted)
            throw new BusinessRuleViolationException("Only submitted work orders can be approved.");

        if (assignedToId == Guid.Empty)
            throw new BusinessRuleViolationException("An active assignee is required.");

        Status = WorkOrderStatus.Approved;
        AssignedToId = assignedToId;
    }

    public void Reject(string reason)
    {
        if (Status != WorkOrderStatus.Submitted)
            throw new BusinessRuleViolationException("Only submitted work orders can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleViolationException("A reason is required to reject a work order.");

        Status = WorkOrderStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    public void Start()
    {
        if (Status != WorkOrderStatus.Approved)
            throw new BusinessRuleViolationException("Only approved work orders can be started.");

        Status = WorkOrderStatus.InProgress;
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (Status != WorkOrderStatus.InProgress)
            throw new BusinessRuleViolationException("Only in-progress work orders can be completed.");

        if (!IsFullyIssued)
            throw new BusinessRuleViolationException(
                "A work order cannot be completed until every requested quantity has been issued.");

        Status = WorkOrderStatus.Completed;
        CompletedDate = completedAtUtc;
    }

    public void Cancel()
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Rejected or WorkOrderStatus.Cancelled)
            throw new BusinessRuleViolationException($"A {Status} work order cannot be cancelled.");

        if (Items.Any(item => item.QuantityIssued > 0))
            throw new BusinessRuleViolationException(
                "A work order with issued stock cannot be cancelled. Return the stock through the ledger first.");

        Status = WorkOrderStatus.Cancelled;
    }
}

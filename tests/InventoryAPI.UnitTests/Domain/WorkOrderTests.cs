using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.UnitTests.Domain;

public class WorkOrderTests
{
    private static WorkOrder CreateDraftWithItems()
    {
        var workOrder = new WorkOrder
        {
            OrderNumber = "WO-20260101-0001",
            Title = "Test order",
            Status = WorkOrderStatus.Draft
        };

        workOrder.Items.Add(new WorkOrderItem
        {
            ProductId = Guid.NewGuid(),
            QuantityRequested = 5
        });

        return workOrder;
    }

    [Fact]
    public void Submit_DraftWithItems_TransitionsToSubmitted()
    {
        var workOrder = CreateDraftWithItems();

        workOrder.Submit();

        workOrder.Status.Should().Be(WorkOrderStatus.Submitted);
    }

    [Fact]
    public void Submit_WithoutItems_Throws()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Draft };

        var act = () => workOrder.Submit();

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*without items*");
    }

    [Theory]
    [InlineData(WorkOrderStatus.Submitted)]
    [InlineData(WorkOrderStatus.Approved)]
    [InlineData(WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.Rejected)]
    public void Submit_NonDraft_Throws(WorkOrderStatus status)
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Status = status;

        var act = () => workOrder.Submit();

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Approve_Submitted_AssignsUserAndTransitions()
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Submit();
        var assignee = Guid.NewGuid();

        workOrder.Approve(assignee);

        workOrder.Status.Should().Be(WorkOrderStatus.Approved);
        workOrder.AssignedToId.Should().Be(assignee);
    }

    [Fact]
    public void Approve_NotSubmitted_Throws()
    {
        var workOrder = CreateDraftWithItems();

        var act = () => workOrder.Approve(Guid.NewGuid());

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Reject_Submitted_StoresReason()
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Submit();

        workOrder.Reject("Budget exceeded");

        workOrder.Status.Should().Be(WorkOrderStatus.Rejected);
        workOrder.RejectionReason.Should().Be("Budget exceeded");
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Submit();

        var act = () => workOrder.Reject("  ");

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*reason*");
    }

    [Fact]
    public void Start_Approved_TransitionsToInProgress()
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Submit();
        workOrder.Approve(Guid.NewGuid());

        workOrder.Start();

        workOrder.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Fact]
    public void Complete_InProgress_SetsCompletedDate()
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Submit();
        workOrder.Approve(Guid.NewGuid());
        workOrder.Start();

        workOrder.Complete();

        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
        workOrder.CompletedDate.Should().NotBeNull();
        workOrder.CompletedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Complete_NotInProgress_Throws()
    {
        var workOrder = CreateDraftWithItems();

        var act = () => workOrder.Complete();

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancel_Completed_Throws()
    {
        var workOrder = CreateDraftWithItems();
        workOrder.Submit();
        workOrder.Approve(Guid.NewGuid());
        workOrder.Start();
        workOrder.Complete();

        var act = () => workOrder.Cancel();

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancel_Draft_TransitionsToCancelled()
    {
        var workOrder = CreateDraftWithItems();

        workOrder.Cancel();

        workOrder.Status.Should().Be(WorkOrderStatus.Cancelled);
    }
}

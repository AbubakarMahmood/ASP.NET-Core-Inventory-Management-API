using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.UnitTests.Domain;

public class WorkOrderTests
{
    private static WorkOrder Draft(int requested = 5)
    {
        var order = new WorkOrder
        {
            OrderNumber = "WO-TEST",
            Title = "Test order",
            Status = WorkOrderStatus.Draft
        };
        order.Items.Add(new WorkOrderItem
        {
            ProductId = Guid.NewGuid(),
            QuantityRequested = requested
        });
        return order;
    }

    [Fact]
    public void Submit_DraftWithValidItems_Transitions()
    {
        var order = Draft();
        order.Submit();
        order.Status.Should().Be(WorkOrderStatus.Submitted);
    }

    [Fact]
    public void Submit_WithoutItems_Throws()
    {
        var order = new WorkOrder { Status = WorkOrderStatus.Draft };
        var act = order.Submit;
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*without items*");
    }

    [Fact]
    public void Submit_DuplicateProduct_Throws()
    {
        var order = Draft();
        order.Items.Add(new WorkOrderItem
        {
            ProductId = order.Items.Single().ProductId,
            QuantityRequested = 1
        });
        var act = order.Submit;
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*only once*");
    }

    [Fact]
    public void Approve_Submitted_AssignsUser()
    {
        var order = Draft();
        order.Submit();
        var assignee = Guid.NewGuid();
        order.Approve(assignee);
        order.Status.Should().Be(WorkOrderStatus.Approved);
        order.AssignedToId.Should().Be(assignee);
    }

    [Fact]
    public void Reject_Submitted_StoresTrimmedReason()
    {
        var order = Draft();
        order.Submit();
        order.Reject("  Not justified  ");
        order.Status.Should().Be(WorkOrderStatus.Rejected);
        order.RejectionReason.Should().Be("Not justified");
    }

    [Fact]
    public void Start_Approved_Transitions()
    {
        var order = Draft();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.Start();
        order.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Fact]
    public void Complete_FullyIssuedOrder_SetsSuppliedTimestamp()
    {
        var order = Draft(3);
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.Start();
        order.Items.Single().Issue(3);
        var completedAt = new DateTime(2026, 7, 26, 1, 2, 3, DateTimeKind.Utc);

        order.Complete(completedAt);

        order.Status.Should().Be(WorkOrderStatus.Completed);
        order.CompletedDate.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_OutstandingQuantity_Throws()
    {
        var order = Draft(3);
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.Start();
        order.Items.Single().Issue(2);
        var act = () => order.Complete(DateTime.UtcNow);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*every requested quantity*");
    }

    [Fact]
    public void Cancel_WithIssuedStock_Throws()
    {
        var order = Draft(3);
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.Start();
        order.Items.Single().Issue(1);
        var act = order.Cancel;
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*issued stock*");
    }

    [Fact]
    public void Cancel_Draft_Transitions()
    {
        var order = Draft();
        order.Cancel();
        order.Status.Should().Be(WorkOrderStatus.Cancelled);
    }

    [Fact]
    public void WorkOrderItem_Issue_PartialAndFinal_TracksRemaining()
    {
        var item = new WorkOrderItem { QuantityRequested = 5 };
        item.Issue(2);
        item.RemainingQuantity.Should().Be(3);
        item.IsFullyIssued.Should().BeFalse();
        item.Issue(3);
        item.RemainingQuantity.Should().Be(0);
        item.IsFullyIssued.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void WorkOrderItem_Issue_InvalidQuantity_Throws(int quantity)
    {
        var item = new WorkOrderItem { QuantityRequested = 5 };
        var act = () => item.Issue(quantity);
        act.Should().Throw<BusinessRuleViolationException>();
        item.QuantityIssued.Should().Be(0);
    }
}

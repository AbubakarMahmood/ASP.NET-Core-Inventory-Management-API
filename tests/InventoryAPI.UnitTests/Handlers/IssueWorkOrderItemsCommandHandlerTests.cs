using System.Linq.Expressions;
using FluentAssertions;
using InventoryAPI.Application.Commands.WorkOrders;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using Moq;

namespace InventoryAPI.UnitTests.Handlers;

public class IssueWorkOrderItemsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWorkOrderRepository> _workOrders = new();
    private readonly Mock<IRepository<StockMovement>> _movements = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Product _product;
    private readonly WorkOrder _workOrder;
    private readonly IssueWorkOrderItemsCommandHandler _handler;

    public IssueWorkOrderItemsCommandHandlerTests()
    {
        _product = ProductWithStock(20, "PART-1");
        _workOrder = InProgressOrder(_product, 8);
        _unitOfWork.SetupGet(unit => unit.WorkOrders).Returns(_workOrders.Object);
        _unitOfWork.SetupGet(unit => unit.StockMovements).Returns(_movements.Object);
        _workOrders.Setup(repository => repository.GetByIdWithDetailsAsync(
                _workOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_workOrder);
        _movements.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<StockMovement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockMovement>());
        _movements.Setup(repository => repository.AddAsync(
                It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockMovement movement, CancellationToken _) => movement);
        _currentUser.Setup(service => service.RequireUserId()).Returns(_actorId);
        _handler = new IssueWorkOrderItemsCommandHandler(_unitOfWork.Object, _currentUser.Object);
    }

    private static Product ProductWithStock(int stock, string sku)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = sku,
            Name = sku,
            UnitOfMeasure = "EA",
            UnitCost = 3m,
            Location = "A-01"
        };
        product.ApplyStockDelta(stock);
        return product;
    }

    private static WorkOrder InProgressOrder(Product product, int requested)
    {
        var order = new WorkOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "WO-TEST",
            Title = "Repair",
            Status = WorkOrderStatus.InProgress,
            RequestedBy = new User { Email = "requester@example.com" }
        };
        order.Items.Add(new WorkOrderItem
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            ProductId = product.Id,
            Product = product,
            WorkOrder = order,
            QuantityRequested = requested
        });
        return order;
    }

    private IssueWorkOrderItemsCommand Command(int quantity = 3) => new()
    {
        OperationId = Guid.NewGuid(),
        WorkOrderId = _workOrder.Id,
        Items = { new IssueItemRequest { ProductId = _product.Id, Quantity = quantity } }
    };

    [Fact]
    public async Task Handle_ValidBatch_PostsMovementAndIssuesItem()
    {
        StockMovement? captured = null;
        _movements.Setup(repository => repository.AddAsync(
                It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((movement, _) => captured = movement)
            .ReturnsAsync((StockMovement movement, CancellationToken _) => movement);
        var command = Command(3);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        _product.CurrentStock.Should().Be(17);
        _workOrder.Items.Single().QuantityIssued.Should().Be(3);
        captured.Should().NotBeNull();
        captured!.OperationId.Should().Be(command.OperationId);
        captured.WorkOrderId.Should().Be(_workOrder.Id);
        captured.BalanceBefore.Should().Be(20);
        captured.BalanceAfter.Should().Be(17);
        result.Items.Single().RemainingQuantity.Should().Be(5);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleLines_PrevalidatesBeforeAnyMutation()
    {
        var second = ProductWithStock(1, "PART-2");
        _workOrder.Items.Add(new WorkOrderItem
        {
            Id = Guid.NewGuid(),
            WorkOrderId = _workOrder.Id,
            ProductId = second.Id,
            Product = second,
            WorkOrder = _workOrder,
            QuantityRequested = 4
        });
        var command = new IssueWorkOrderItemsCommand
        {
            OperationId = Guid.NewGuid(),
            WorkOrderId = _workOrder.Id,
            Items =
            {
                new IssueItemRequest { ProductId = _product.Id, Quantity = 3 },
                new IssueItemRequest { ProductId = second.Id, Quantity = 2 }
            }
        };

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InsufficientStockException>();
        _product.CurrentStock.Should().Be(20);
        second.CurrentStock.Should().Be(1);
        _workOrder.Items.Should().OnlyContain(item => item.QuantityIssued == 0);
        _movements.Verify(repository => repository.AddAsync(
            It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EquivalentRetry_ReturnsCurrentOrderWithoutSaving()
    {
        var command = Command(3);
        var prior = StockMovement.Post(
            _product, command.OperationId, StockMovementType.Issue, 3,
            "Issued for work order WO-TEST", "WO-TEST", _actorId, _workOrder.Id);
        _workOrder.Items.Single().Issue(3);
        _movements.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<StockMovement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { prior });

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        result.Items.Single().QuantityIssued.Should().Be(3);
        _product.CurrentStock.Should().Be(17);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReusedOperationWithDifferentQuantity_ThrowsConflict()
    {
        var command = Command(4);
        var prior = StockMovement.Post(
            _product, command.OperationId, StockMovementType.Issue, 3,
            "Issued for work order WO-TEST", "WO-TEST", _actorId, _workOrder.Id);
        _movements.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<StockMovement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { prior });

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<IdempotencyConflictException>();
    }

    [Fact]
    public async Task Handle_ExceedsRemainingQuantity_RejectsWithoutMutation()
    {
        var act = () => _handler.Handle(Command(9), TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<BusinessRuleViolationException>().WithMessage("*Remaining*");
        _product.CurrentStock.Should().Be(20);
    }

    [Fact]
    public async Task Handle_NonInProgressOrder_Rejects()
    {
        _workOrder.Status = WorkOrderStatus.Approved;
        var act = () => _handler.Handle(Command(), TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<BusinessRuleViolationException>().WithMessage("*in-progress*");
    }

    [Fact]
    public async Task Handle_ProductNotOnOrder_ThrowsNotFound()
    {
        var command = Command();
        command.Items[0].ProductId = Guid.NewGuid();
        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateProductInRequest_ThrowsValidation()
    {
        var command = Command();
        command.Items.Add(new IssueItemRequest { ProductId = _product.Id, Quantity = 1 });
        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ValidationException>();
        _product.CurrentStock.Should().Be(20);
    }
}

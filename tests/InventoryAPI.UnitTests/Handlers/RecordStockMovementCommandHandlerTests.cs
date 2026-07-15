using System.Linq.Expressions;
using FluentAssertions;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using Moq;

namespace InventoryAPI.UnitTests.Handlers;

public class RecordStockMovementCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Product>> _products = new();
    private readonly Mock<IRepository<StockMovement>> _movements = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly RecordStockMovementCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Product _product;

    public RecordStockMovementCommandHandlerTests()
    {
        _product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "TEST-001",
            Name = "Test product",
            CurrentStock = 100,
            UnitCost = 2.50m,
            Location = "A-01"
        };

        _unitOfWork.SetupGet(u => u.Products).Returns(_products.Object);
        _unitOfWork.SetupGet(u => u.StockMovements).Returns(_movements.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);

        _products.Setup(r => r.GetByIdAsync(_product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
        _users.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, FirstName = "Test", LastName = "User" });
        _currentUser.Setup(c => c.RequireUserId()).Returns(_userId);

        _handler = new RecordStockMovementCommandHandler(_unitOfWork.Object, _currentUser.Object);
    }

    private RecordStockMovementCommand Command(StockMovementType type, int quantity) => new()
    {
        ProductId = _product.Id,
        Type = type,
        Quantity = quantity,
        Reason = "Test",
        SourceLocation = "A-01",
        DestinationLocation = "B-02"
    };

    [Fact]
    public async Task Handle_Receipt_IncreasesStock()
    {
        await _handler.Handle(Command(StockMovementType.Receipt, 20), default);

        _product.CurrentStock.Should().Be(120);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Issue_DecreasesStock()
    {
        await _handler.Handle(Command(StockMovementType.Issue, 30), default);

        _product.CurrentStock.Should().Be(70);
    }

    [Fact]
    public async Task Handle_IssueMoreThanAvailable_ThrowsInsufficientStock()
    {
        var act = () => _handler.Handle(Command(StockMovementType.Issue, 101), default);

        await act.Should().ThrowAsync<InsufficientStockException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NegativeAdjustment_DecreasesStock()
    {
        await _handler.Handle(Command(StockMovementType.Adjustment, -25), default);

        _product.CurrentStock.Should().Be(75);
    }

    [Fact]
    public async Task Handle_PositiveAdjustment_IncreasesStock()
    {
        await _handler.Handle(Command(StockMovementType.Adjustment, 25), default);

        _product.CurrentStock.Should().Be(125);
    }

    [Fact]
    public async Task Handle_Transfer_ChangesLocationWithoutTouchingStock()
    {
        await _handler.Handle(Command(StockMovementType.Transfer, 10), default);

        _product.CurrentStock.Should().Be(100);
        _product.Location.Should().Be("B-02");
    }

    [Fact]
    public async Task Handle_UnknownProduct_ThrowsNotFound()
    {
        var command = Command(StockMovementType.Receipt, 1);
        command.ProductId = Guid.NewGuid();

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownWorkOrderReference_ThrowsNotFound()
    {
        var workOrders = new Mock<IWorkOrderRepository>();
        workOrders.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkOrder, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.SetupGet(u => u.WorkOrders).Returns(workOrders.Object);

        var command = Command(StockMovementType.Issue, 1);
        command.WorkOrderId = Guid.NewGuid();

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RecordsUnitCostAtTransactionTime()
    {
        StockMovement? captured = null;
        _movements.Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync((StockMovement m, CancellationToken _) => m);

        await _handler.Handle(Command(StockMovementType.Receipt, 5), default);

        captured.Should().NotBeNull();
        captured!.UnitCostAtTransaction.Should().Be(2.50m);
        captured.PerformedById.Should().Be(_userId);
    }
}

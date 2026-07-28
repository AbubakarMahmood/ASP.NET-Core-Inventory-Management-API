using System.Linq.Expressions;
using FluentAssertions;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryAPI.UnitTests.Handlers;

public class RecordStockMovementCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Product>> _products = new();
    private readonly Mock<IRepository<StockMovement>> _movements = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Product _product;
    private readonly RecordStockMovementCommandHandler _handler;

    public RecordStockMovementCommandHandlerTests()
    {
        _product = ProductWithStock(100);
        _unitOfWork.SetupGet(unit => unit.Products).Returns(_products.Object);
        _unitOfWork.SetupGet(unit => unit.StockMovements).Returns(_movements.Object);
        _unitOfWork.SetupGet(unit => unit.Users).Returns(_users.Object);
        _movements.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<StockMovement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockMovement>());
        _movements.Setup(repository => repository.AddAsync(
                It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockMovement movement, CancellationToken _) => movement);
        _products.Setup(repository => repository.GetByIdAsync(_product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
        _users.Setup(repository => repository.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, FirstName = "Test", LastName = "User" });
        _currentUser.Setup(service => service.RequireUserId()).Returns(_userId);
        _handler = new RecordStockMovementCommandHandler(_unitOfWork.Object, _currentUser.Object);
    }

    private static Product ProductWithStock(int stock)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "TEST-001",
            Name = "Test product",
            UnitOfMeasure = "EA",
            UnitCost = 2.50m,
            Location = "A-01"
        };
        product.ApplyStockDelta(stock);
        return product;
    }

    private RecordStockMovementCommand Command(StockMovementType type, int quantity) => new()
    {
        OperationId = Guid.NewGuid(),
        ProductId = _product.Id,
        Type = type,
        Quantity = quantity,
        Reason = "  Test movement  ",
        Reference = "  REF-1  "
    };

    [Fact]
    public async Task Handle_Receipt_PostsMovementAndSnapshots()
    {
        StockMovement? captured = null;
        _movements.Setup(repository => repository.AddAsync(
                It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((movement, _) => captured = movement)
            .ReturnsAsync((StockMovement movement, CancellationToken _) => movement);

        var command = Command(StockMovementType.Receipt, 20);
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        _product.CurrentStock.Should().Be(120);
        captured.Should().NotBeNull();
        captured!.OperationId.Should().Be(command.OperationId);
        captured.BalanceBefore.Should().Be(100);
        captured.BalanceAfter.Should().Be(120);
        captured.Reason.Should().Be("Test movement");
        captured.Reference.Should().Be("REF-1");
        result.ProductSKU.Should().Be("TEST-001");
        result.PerformedByName.Should().Be("Test User");
        _products.Verify(repository => repository.Update(_product), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(StockMovementType.Issue, 30, 70)]
    [InlineData(StockMovementType.Adjustment, -25, 75)]
    [InlineData(StockMovementType.Return, 5, 105)]
    public async Task Handle_SupportedType_UpdatesBalance(
        StockMovementType type, int quantity, int expected)
    {
        await _handler.Handle(Command(type, quantity), TestContext.Current.CancellationToken);
        _product.CurrentStock.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_IssueBeyondAvailable_ThrowsWithoutSaving()
    {
        var act = () => _handler.Handle(
            Command(StockMovementType.Issue, 101),
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InsufficientStockException>();
        _product.CurrentStock.Should().Be(100);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StockMovementType.Transfer)]
    [InlineData(StockMovementType.OpeningBalance)]
    public async Task Handle_ReservedType_IsRejected(StockMovementType type)
    {
        var act = () => _handler.Handle(Command(type, 1), TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _product.CurrentStock.Should().Be(100);
    }

    [Fact]
    public async Task Handle_EquivalentRetry_ReturnsOriginalWithoutSaving()
    {
        var command = Command(StockMovementType.Issue, 5);
        var replayProduct = ProductWithStock(100);
        replayProduct.Id = _product.Id;
        var prior = StockMovement.Post(
            replayProduct, command.OperationId, command.Type, command.Quantity,
            "Test movement", "REF-1", _userId);
        _movements.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<StockMovement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { prior });
        _products.Setup(repository => repository.GetByIdAsync(_product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(replayProduct);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        result.Id.Should().Be(prior.Id);
        replayProduct.CurrentStock.Should().Be(95);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReusedOperationForDifferentPayload_ThrowsConflict()
    {
        var command = Command(StockMovementType.Issue, 5);
        var priorProduct = ProductWithStock(100);
        priorProduct.Id = _product.Id;
        var prior = StockMovement.Post(
            priorProduct, command.OperationId, command.Type, 4,
            "Test movement", "REF-1", _userId);
        _movements.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<StockMovement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { prior });

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<IdempotencyConflictException>();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownProduct_ThrowsNotFound()
    {
        var command = Command(StockMovementType.Receipt, 1);
        command.ProductId = Guid.NewGuid();
        _products.Setup(repository => repository.GetByIdAsync(command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ConcurrencyFailure_MapsToConflict()
    {
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var act = () => _handler.Handle(
            Command(StockMovementType.Receipt, 1),
            TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}

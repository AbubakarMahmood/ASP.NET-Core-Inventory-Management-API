using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.UnitTests.Domain;

public class StockMovementTests
{
    private readonly Guid _actorId = Guid.NewGuid();

    private static Product Product(int stock = 0)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "PART-001",
            Name = "Part",
            Location = "BIN-A",
            UnitOfMeasure = "EA",
            UnitCost = 2.50m
        };
        if (stock > 0)
        {
            product.ApplyStockDelta(stock);
        }
        return product;
    }

    [Fact]
    public void Post_OpeningBalance_RecordsSnapshotsAndLocations()
    {
        var product = Product();
        var operationId = Guid.NewGuid();

        var movement = StockMovement.Post(
            product, operationId, StockMovementType.OpeningBalance, 12,
            "Opening balance", null, _actorId);

        product.CurrentStock.Should().Be(12);
        movement.OperationId.Should().Be(operationId);
        movement.BalanceBefore.Should().Be(0);
        movement.BalanceAfter.Should().Be(12);
        movement.SourceLocation.Should().Be(StockMovement.OpeningBalanceSource);
        movement.DestinationLocation.Should().Be("BIN-A");
        movement.UnitCostAtTransaction.Should().Be(2.50m);
    }

    [Theory]
    [InlineData(StockMovementType.Receipt, 5, 15)]
    [InlineData(StockMovementType.Return, 5, 15)]
    [InlineData(StockMovementType.Issue, 5, 5)]
    [InlineData(StockMovementType.Adjustment, 5, 15)]
    [InlineData(StockMovementType.Adjustment, -5, 5)]
    public void Post_SupportedMovement_AppliesExpectedDelta(
        StockMovementType type, int quantity, int expected)
    {
        var product = Product(10);
        var movement = StockMovement.Post(
            product, Guid.NewGuid(), type, quantity, "Reason", "REF", _actorId);

        product.CurrentStock.Should().Be(expected);
        movement.BalanceBefore.Should().Be(10);
        movement.BalanceAfter.Should().Be(expected);
        movement.Reference.Should().Be("REF");
    }

    [Fact]
    public void Post_Transfer_IsRejectedWithoutMutation()
    {
        var product = Product(10);
        var act = () => StockMovement.Post(
            product, Guid.NewGuid(), StockMovementType.Transfer, 10,
            "Move", null, _actorId);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*not supported*");
        product.CurrentStock.Should().Be(10);
        product.Location.Should().Be("BIN-A");
    }

    [Fact]
    public void Post_IssueBeyondBalance_ThrowsWithoutMutation()
    {
        var product = Product(3);
        var act = () => StockMovement.Post(
            product, Guid.NewGuid(), StockMovementType.Issue, 4,
            "Issue", null, _actorId);

        act.Should().Throw<InsufficientStockException>();
        product.CurrentStock.Should().Be(3);
    }

    [Theory]
    [InlineData(StockMovementType.Receipt, 0)]
    [InlineData(StockMovementType.Receipt, -1)]
    [InlineData(StockMovementType.Issue, -1)]
    [InlineData(StockMovementType.Adjustment, 0)]
    [InlineData(StockMovementType.OpeningBalance, 0)]
    public void CalculateQuantityDelta_InvalidQuantity_Throws(StockMovementType type, int quantity)
    {
        var act = () => StockMovement.CalculateQuantityDelta(type, quantity);
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void CalculateQuantityDelta_HistoricalTransfer_IsZero()
    {
        StockMovement.CalculateQuantityDelta(StockMovementType.Transfer, 25).Should().Be(0);
    }

    [Fact]
    public void Post_RequiresOperationActorAndReason()
    {
        var product = Product();
        var noOperation = () => StockMovement.Post(product, Guid.Empty, StockMovementType.Receipt, 1, "R", null, _actorId);
        var noActor = () => StockMovement.Post(product, Guid.NewGuid(), StockMovementType.Receipt, 1, "R", null, Guid.Empty);
        var noReason = () => StockMovement.Post(product, Guid.NewGuid(), StockMovementType.Receipt, 1, " ", null, _actorId);

        noOperation.Should().Throw<BusinessRuleViolationException>();
        noActor.Should().Throw<BusinessRuleViolationException>();
        noReason.Should().Throw<BusinessRuleViolationException>();
    }
}

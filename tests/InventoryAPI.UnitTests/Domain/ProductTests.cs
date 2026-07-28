using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.UnitTests.Domain;

public class ProductTests
{
    private static Product CreateProduct(int stock = 0, int reorderPoint = 5)
    {
        var product = new Product
        {
            SKU = "TEST-001",
            Name = "Test product",
            Location = "A-01",
            UnitOfMeasure = "EA",
            ReorderPoint = reorderPoint
        };
        if (stock != 0)
        {
            product.ApplyStockDelta(stock);
        }
        return product;
    }

    [Fact]
    public void ApplyStockDelta_Positive_IncreasesBalance()
    {
        var product = CreateProduct(10);
        product.ApplyStockDelta(5);
        product.CurrentStock.Should().Be(15);
    }

    [Fact]
    public void ApplyStockDelta_Negative_DecreasesBalance()
    {
        var product = CreateProduct(10);
        product.ApplyStockDelta(-4);
        product.CurrentStock.Should().Be(6);
    }

    [Fact]
    public void ApplyStockDelta_ToZero_Succeeds()
    {
        var product = CreateProduct(10);
        product.ApplyStockDelta(-10);
        product.CurrentStock.Should().Be(0);
    }

    [Fact]
    public void ApplyStockDelta_BelowZero_ThrowsWithoutMutation()
    {
        var product = CreateProduct(3);
        var act = () => product.ApplyStockDelta(-4);
        act.Should().Throw<InsufficientStockException>()
            .Which.Available.Should().Be(3);
        product.CurrentStock.Should().Be(3);
    }

    [Fact]
    public void ApplyStockDelta_Overflow_ThrowsWithoutMutation()
    {
        var product = CreateProduct(int.MaxValue);
        var act = () => product.ApplyStockDelta(1);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*overflow*");
        product.CurrentStock.Should().Be(int.MaxValue);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(4, 5, true)]
    [InlineData(6, 5, false)]
    public void IsLowStock_UsesInclusiveReorderPoint(int stock, int reorderPoint, bool expected)
    {
        CreateProduct(stock, reorderPoint).IsLowStock().Should().Be(expected);
    }
}

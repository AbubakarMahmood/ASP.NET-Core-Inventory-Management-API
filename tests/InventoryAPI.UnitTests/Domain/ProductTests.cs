using FluentAssertions;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;

namespace InventoryAPI.UnitTests.Domain;

public class ProductTests
{
    private static Product CreateProduct(int currentStock = 10, int reorderPoint = 5) => new()
    {
        SKU = "TEST-001",
        Name = "Test product",
        CurrentStock = currentStock,
        ReorderPoint = reorderPoint
    };

    [Fact]
    public void AdjustStock_PositiveQuantity_IncreasesStock()
    {
        var product = CreateProduct(currentStock: 10);

        product.AdjustStock(5);

        product.CurrentStock.Should().Be(15);
    }

    [Fact]
    public void AdjustStock_NegativeQuantity_DecreasesStock()
    {
        var product = CreateProduct(currentStock: 10);

        product.AdjustStock(-4);

        product.CurrentStock.Should().Be(6);
    }

    [Fact]
    public void AdjustStock_ToExactlyZero_Succeeds()
    {
        var product = CreateProduct(currentStock: 10);

        product.AdjustStock(-10);

        product.CurrentStock.Should().Be(0);
    }

    [Fact]
    public void AdjustStock_BelowZero_ThrowsAndLeavesStockUnchanged()
    {
        var product = CreateProduct(currentStock: 3);

        var act = () => product.AdjustStock(-4);

        act.Should().Throw<InsufficientStockException>()
            .Which.Available.Should().Be(3);
        product.CurrentStock.Should().Be(3);
    }

    [Theory]
    [InlineData(5, 5, true)]   // at the reorder point
    [InlineData(4, 5, true)]   // below
    [InlineData(6, 5, false)]  // above
    public void IsLowStock_ComparesAgainstReorderPoint(int stock, int reorderPoint, bool expected)
    {
        var product = CreateProduct(stock, reorderPoint);

        product.IsLowStock().Should().Be(expected);
    }
}

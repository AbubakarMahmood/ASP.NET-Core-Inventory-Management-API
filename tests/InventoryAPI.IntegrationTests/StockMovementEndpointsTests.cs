using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public class StockMovementEndpointsTests : ApiTestBase
{
    public StockMovementEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<ProductDto> CreateProductAsync(HttpClient client, int currentStock)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"SM-{Guid.NewGuid():N}"[..20],
            name = "Stock movement test product",
            description = "d",
            category = "Test",
            currentStock,
            reorderPoint = 1,
            reorderQuantity = 5,
            unitOfMeasure = "EA",
            unitCost = 4.20,
            location = "S-01-01"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions))!;
    }

    private async Task<ProductDto> GetProductAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<ProductDto>($"/api/v1/products/{id}", JsonOptions))!;

    [Fact]
    public async Task RecordReceipt_IncreasesProductStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 10);

        var response = await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Receipt",
            quantity = 15,
            reason = "Purchase order received"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var updated = await GetProductAsync(client, product.Id);
        updated.CurrentStock.Should().Be(25);
    }

    [Fact]
    public async Task RecordIssue_DecreasesProductStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 10);

        var response = await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Issue",
            quantity = 4,
            reason = "Issued to floor"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var updated = await GetProductAsync(client, product.Id);
        updated.CurrentStock.Should().Be(6);
    }

    [Fact]
    public async Task RecordIssue_MoreThanAvailable_Returns400AndLeavesStockUnchanged()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 3);

        var response = await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Issue",
            quantity = 10,
            reason = "Too much"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var updated = await GetProductAsync(client, product.Id);
        updated.CurrentStock.Should().Be(3);
    }

    [Fact]
    public async Task RecordNegativeAdjustment_DecreasesStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 20);

        var response = await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Adjustment",
            quantity = -8,
            reason = "Cycle count correction"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var updated = await GetProductAsync(client, product.Id);
        updated.CurrentStock.Should().Be(12);
    }

    [Fact]
    public async Task RecordZeroQuantity_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 5);

        var response = await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Receipt",
            quantity = 0,
            reason = "Nothing"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordTransfer_UpdatesLocationWithoutChangingStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 7);

        var response = await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Transfer",
            quantity = 7,
            sourceLocation = "S-01-01",
            destinationLocation = "S-02-02",
            reason = "Relocation"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var updated = await GetProductAsync(client, product.Id);
        updated.CurrentStock.Should().Be(7);
        updated.Location.Should().Be("S-02-02");
    }

    [Fact]
    public async Task GetMovementsForProduct_ReturnsRecordedMovements()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 10);

        await client.PostAsJsonAsync("/api/v1/stockmovements", new
        {
            productId = product.Id,
            type = "Receipt",
            quantity = 5,
            reason = "Restock"
        });

        var result = await client.GetFromJsonAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements/product/{product.Id}", JsonOptions);

        result!.Items.Should().HaveCount(1);
        result.Items[0].ProductSKU.Should().Be(product.SKU);
        result.Items[0].PerformedByName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Statistics_WithNoMovementsInRange_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/v1/stockmovements/statistics?fromDate=1990-01-01&toDate=1990-01-02");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<StockMovementStatisticsDto>(JsonOptions);
        stats!.TotalMovements.Should().Be(0);
    }
}

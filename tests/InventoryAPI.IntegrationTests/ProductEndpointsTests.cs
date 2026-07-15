using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public class ProductEndpointsTests : ApiTestBase
{
    public ProductEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_Authenticated_ReturnsSeededProducts()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<PaginatedResult<ProductDto>>(
            "/api/v1/products?pageSize=50", JsonOptions);

        result!.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(p => p.SKU == "WIDGET-001");
        result.TotalCount.Should().BeGreaterThanOrEqualTo(result.Items.Count);
    }

    [Fact]
    public async Task GetProducts_LowStockFilter_OnlyReturnsLowStockItems()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<PaginatedResult<ProductDto>>(
            "/api/v1/products?lowStockOnly=true&pageSize=50", JsonOptions);

        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(p => p.IsLowStock);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_Returns201WithLocation()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"IT-{Guid.NewGuid():N}"[..20],
            name = "Integration test product",
            description = "Created by an integration test",
            category = "Test",
            currentStock = 10,
            reorderPoint = 2,
            reorderQuantity = 5,
            unitOfMeasure = "EA",
            unitCost = 9.99,
            location = "T-01-01"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        var fetched = await client.GetFromJsonAsync<ProductDto>(
            $"/api/v1/products/{created!.Id}", JsonOptions);
        fetched!.Name.Should().Be("Integration test product");
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "WIDGET-001", // seeded SKU
            name = "Duplicate",
            description = "d",
            category = "Test",
            currentStock = 1,
            reorderPoint = 1,
            reorderQuantity = 1,
            unitOfMeasure = "EA",
            unitCost = 1.00,
            location = "T-01-02"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPayload_Returns400WithFieldErrors()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "",
            name = "",
            category = "Test",
            currentStock = -5,
            reorderPoint = 0,
            reorderQuantity = 0,
            unitOfMeasure = "EA",
            unitCost = 0,
            location = "T-01-03"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task CreateProduct_AsOperator_Returns403()
    {
        var client = await CreateAuthenticatedClientAsync("operator@inventory.com", "Operator123!");

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "OP-DENIED-001",
            name = "Should be denied",
            description = "d",
            category = "Test",
            currentStock = 1,
            reorderPoint = 1,
            reorderQuantity = 1,
            unitOfMeasure = "EA",
            unitCost = 1.00,
            location = "T-01-04"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProduct_UnknownId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_SoftDeletesAndHidesFromQueries()
    {
        var client = await CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"DEL-{Guid.NewGuid():N}"[..20],
            name = "To be deleted",
            description = "d",
            category = "Test",
            currentStock = 1,
            reorderPoint = 1,
            reorderQuantity = 1,
            unitOfMeasure = "EA",
            unitCost = 1.00,
            location = "T-01-05"
        });
        var created = await create.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);

        var delete = await client.DeleteAsync($"/api/v1/products/{created!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetch = await client.GetAsync($"/api/v1/products/{created.Id}");
        fetch.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

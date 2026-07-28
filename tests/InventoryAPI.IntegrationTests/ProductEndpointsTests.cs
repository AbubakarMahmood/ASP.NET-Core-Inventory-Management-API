using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public class ProductEndpointsTests : ApiTestBase
{
    public ProductEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_WithOpeningStock_PostsOpeningLedgerEntry()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 7);

        product.CurrentStock.Should().Be(7);
        product.Version.Should().BeGreaterThan(0);

        var movements = await client.GetFromJsonAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements/product/{product.Id}",
            JsonOptions,
            TestContext.Current.CancellationToken);

        movements!.Items.Should().ContainSingle();
        var opening = movements.Items.Single();
        opening.Type.Should().Be(StockMovementType.OpeningBalance);
        opening.Quantity.Should().Be(7);
        opening.BalanceBefore.Should().Be(0);
        opening.BalanceAfter.Should().Be(7);
    }

    [Fact]
    public async Task Update_ChangesMetadataButCannotAcceptStockField()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 5);

        var update = await client.PutAsJsonAsync(
            $"/api/v1/products/{product.Id}",
            new
            {
                sku = product.SKU,
                name = "Renamed product",
                description = product.Description,
                category = product.Category,
                reorderPoint = product.ReorderPoint,
                reorderQuantity = product.ReorderQuantity,
                unitOfMeasure = product.UnitOfMeasure,
                unitCost = product.UnitCost,
                location = "TEST-02",
                version = product.Version
            },
            TestContext.Current.CancellationToken);

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<ProductDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        updated.Name.Should().Be("Renamed product");
        updated.Location.Should().Be("TEST-02");
        updated.CurrentStock.Should().Be(5);

        var attemptToEditStock = await client.PutAsJsonAsync(
            $"/api/v1/products/{product.Id}",
            new
            {
                sku = updated.SKU,
                name = updated.Name,
                description = updated.Description,
                category = updated.Category,
                currentStock = 999,
                reorderPoint = updated.ReorderPoint,
                reorderQuantity = updated.ReorderQuantity,
                unitOfMeasure = updated.UnitOfMeasure,
                unitCost = updated.UnitCost,
                location = updated.Location,
                version = updated.Version
            },
            TestContext.Current.CancellationToken);

        attemptToEditStock.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetProductAsync(client, product.Id))!.CurrentStock.Should().Be(5);
    }

    [Fact]
    public async Task Update_WithStaleVersion_IsRejected()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 0);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/products/{product.Id}",
            new
            {
                sku = product.SKU,
                name = product.Name,
                description = product.Description,
                category = product.Category,
                reorderPoint = product.ReorderPoint,
                reorderQuantity = product.ReorderQuantity,
                unitOfMeasure = product.UnitOfMeasure,
                unitCost = product.UnitCost,
                location = product.Location,
                version = product.Version + 1
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ConcurrentUpdates_WithSameVersion_AllowOnlyOneWinner()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 0);

        object UpdatePayload(string name) => new
        {
            sku = product.SKU,
            name,
            description = product.Description,
            category = product.Category,
            reorderPoint = product.ReorderPoint,
            reorderQuantity = product.ReorderQuantity,
            unitOfMeasure = product.UnitOfMeasure,
            unitCost = product.UnitCost,
            location = product.Location,
            version = product.Version
        };

        var responses = await Task.WhenAll(
            client.PutAsJsonAsync(
                $"/api/v1/products/{product.Id}",
                UpdatePayload("Concurrent winner A"),
                TestContext.Current.CancellationToken),
            client.PutAsJsonAsync(
                $"/api/v1/products/{product.Id}",
                UpdatePayload("Concurrent winner B"),
                TestContext.Current.CancellationToken));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        var persisted = await GetProductAsync(client, product.Id);
        persisted!.Name.Should().BeOneOf("Concurrent winner A", "Concurrent winner B");
        persisted.Version.Should().NotBe(product.Version);
    }

    [Fact]
    public async Task ProductWithLedgerHistory_CannotBeDeleted()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 1);

        var response = await client.DeleteAsync(
            $"/api/v1/products/{product.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Exports_ReturnReadableXlsxAndPdfPayloads()
    {
        var client = await CreateAuthenticatedClientAsync();
        await CreateProductAsync(client, openingStock: 4);

        var xlsx = await client.GetAsync(
            "/api/v1/products/export",
            TestContext.Current.CancellationToken);
        xlsx.StatusCode.Should().Be(HttpStatusCode.OK);
        xlsx.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var xlsxBytes = await xlsx.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        xlsxBytes[..2].Should().Equal(0x50, 0x4B);

        var pdf = await client.GetAsync(
            "/api/v1/products/export/pdf",
            TestContext.Current.CancellationToken);
        pdf.StatusCode.Should().Be(HttpStatusCode.OK);
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var pdfBytes = await pdf.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }
}

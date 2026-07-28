using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Infrastructure.Data;
using InventoryAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace InventoryAPI.IntegrationTests;

public class StockMovementEndpointsTests : ApiTestBase
{
    public StockMovementEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Receipt_IsIdempotent_AndConflictingReuseReturns409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 10);
        var operationId = Guid.NewGuid();
        var request = new
        {
            operationId,
            productId = product.Id,
            type = "Receipt",
            quantity = 5,
            reason = "Supplier receipt",
            reference = "PO-100"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/stockmovements",
            request,
            TestContext.Current.CancellationToken);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var first = (await firstResponse.Content.ReadFromJsonAsync<StockMovementDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        first.BalanceBefore.Should().Be(10);
        first.BalanceAfter.Should().Be(15);

        var replayResponse = await client.PostAsJsonAsync(
            "/api/v1/stockmovements",
            request,
            TestContext.Current.CancellationToken);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var replay = (await replayResponse.Content.ReadFromJsonAsync<StockMovementDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        replay.Id.Should().Be(first.Id);
        replay.BalanceAfter.Should().Be(15);

        var conflict = await client.PostAsJsonAsync(
            "/api/v1/stockmovements",
            new
            {
                operationId,
                productId = product.Id,
                type = "Receipt",
                quantity = 6,
                reason = "Supplier receipt",
                reference = "PO-100"
            },
            TestContext.Current.CancellationToken);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await GetProductAsync(client, product.Id))!.CurrentStock.Should().Be(15);
    }

    [Fact]
    public async Task UnsupportedTransfer_IsRejectedWithoutChangingBalance()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 7);

        var response = await client.PostAsJsonAsync(
            "/api/v1/stockmovements",
            new
            {
                operationId = Guid.NewGuid(),
                productId = product.Id,
                type = "Transfer",
                quantity = 7,
                reason = "Move stock"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetProductAsync(client, product.Id))!.CurrentStock.Should().Be(7);
    }

    [Fact]
    public async Task ConcurrentEquivalentReceipts_ApplyTheBalanceChangeOnce()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 10);
        var operationId = Guid.NewGuid();
        var request = new
        {
            operationId,
            productId = product.Id,
            type = "Receipt",
            quantity = 5,
            reason = "Concurrent supplier receipt",
            reference = "PO-CONCURRENT"
        };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(
                "/api/v1/stockmovements",
                request,
                TestContext.Current.CancellationToken),
            client.PostAsJsonAsync(
                "/api/v1/stockmovements",
                request,
                TestContext.Current.CancellationToken));

        responses.Should().OnlyContain(response =>
            response.StatusCode == HttpStatusCode.Created
            || response.StatusCode == HttpStatusCode.Conflict);
        responses.Should().Contain(response => response.StatusCode == HttpStatusCode.Created);
        (await GetProductAsync(client, product.Id))!.CurrentStock.Should().Be(15);

        var movements = await client.GetFromJsonAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements/product/{product.Id}",
            JsonOptions,
            TestContext.Current.CancellationToken);
        movements!.Items.Count(item => item.OperationId == operationId).Should().Be(1);
    }

    [Fact]
    public async Task IssueBeyondAvailableStock_IsAtomic()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 3);

        var response = await client.PostAsJsonAsync(
            "/api/v1/stockmovements",
            new
            {
                operationId = Guid.NewGuid(),
                productId = product.Id,
                type = "Issue",
                quantity = 4,
                reason = "Over-issue attempt"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetProductAsync(client, product.Id))!.CurrentStock.Should().Be(3);

        var movements = await client.GetFromJsonAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements/product/{product.Id}",
            JsonOptions,
            TestContext.Current.CancellationToken);
        movements!.Items.Should().ContainSingle(item => item.Type == StockMovementType.OpeningBalance);
    }

    [Fact]
    public async Task PersistedLedgerRow_CannotBeUpdatedOrDeleted()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 2);

        var movements = await client.GetFromJsonAsync<PaginatedResult<StockMovementDto>>(
            $"/api/v1/stockmovements/product/{product.Id}",
            JsonOptions,
            TestContext.Current.CancellationToken);
        var movementId = movements!.Items.Single().Id;

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const string tamperedReason = "Tampered";
        Func<Task> update = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"StockMovements\" SET \"Reason\" = {tamperedReason} WHERE \"Id\" = {movementId}",
            TestContext.Current.CancellationToken);
        await update.Should().ThrowAsync<PostgresException>();

        context.ChangeTracker.Clear();
        Func<Task> delete = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"StockMovements\" WHERE \"Id\" = {movementId}",
            TestContext.Current.CancellationToken);
        await delete.Should().ThrowAsync<PostgresException>();
    }
}

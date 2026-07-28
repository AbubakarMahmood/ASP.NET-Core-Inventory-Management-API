using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public class WorkOrderLifecycleTests : ApiTestBase
{
    public WorkOrderLifecycleTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Fulfilment_IsAtomicIdempotent_AndCompletionRequiresAllRequestedStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 20);
        var draft = await CreateDraftAsync(client, product.Id, quantity: 10);

        await SubmitApproveAndStartAsync(client, draft.Id);

        var prematureCompletion = await client.PostAsync(
            $"/api/v1/workorders/{draft.Id}/complete",
            null,
            TestContext.Current.CancellationToken);
        prematureCompletion.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var firstOperation = Guid.NewGuid();
        var issueFour = new
        {
            operationId = firstOperation,
            items = new[] { new { productId = product.Id, quantity = 4, notes = "First pick" } }
        };

        var firstIssueResponse = await client.PostAsJsonAsync(
            $"/api/v1/workorders/{draft.Id}/issue-items",
            issueFour,
            TestContext.Current.CancellationToken);
        firstIssueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issued = (await firstIssueResponse.Content.ReadFromJsonAsync<WorkOrderDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        issued.Items.Single().QuantityIssued.Should().Be(4);

        var replayResponse = await client.PostAsJsonAsync(
            $"/api/v1/workorders/{draft.Id}/issue-items",
            issueFour,
            TestContext.Current.CancellationToken);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var replay = (await replayResponse.Content.ReadFromJsonAsync<WorkOrderDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        replay.Items.Single().QuantityIssued.Should().Be(4);

        var conflictingReplay = await client.PostAsJsonAsync(
            $"/api/v1/workorders/{draft.Id}/issue-items",
            new
            {
                operationId = firstOperation,
                items = new[] { new { productId = product.Id, quantity = 5 } }
            },
            TestContext.Current.CancellationToken);
        conflictingReplay.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var secondIssueResponse = await client.PostAsJsonAsync(
            $"/api/v1/workorders/{draft.Id}/issue-items",
            new
            {
                operationId = Guid.NewGuid(),
                items = new[] { new { productId = product.Id, quantity = 6 } }
            },
            TestContext.Current.CancellationToken);
        secondIssueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fullyIssued = (await secondIssueResponse.Content.ReadFromJsonAsync<WorkOrderDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        fullyIssued.IsFullyIssued.Should().BeTrue();
        fullyIssued.Items.Single().QuantityIssued.Should().Be(10);

        var complete = await client.PostAsync(
            $"/api/v1/workorders/{draft.Id}/complete",
            null,
            TestContext.Current.CancellationToken);
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = (await complete.Content.ReadFromJsonAsync<WorkOrderDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        completed.Status.Should().Be(WorkOrderStatus.Completed);
        completed.CompletedDate.Should().NotBeNull();

        (await GetProductAsync(client, product.Id))!.CurrentStock.Should().Be(10);
    }

    [Fact]
    public async Task MultiLineIssue_PrevalidatesEntireBatchBeforeChangingAnyBalance()
    {
        var client = await CreateAuthenticatedClientAsync();
        var productA = await CreateProductAsync(client, openingStock: 10);
        var productB = await CreateProductAsync(client, openingStock: 2);
        var draft = await CreateDraftAsync(client, new[]
        {
            new { productId = productA.Id, quantityRequested = 5 },
            new { productId = productB.Id, quantityRequested = 5 }
        });

        await SubmitApproveAndStartAsync(client, draft.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/workorders/{draft.Id}/issue-items",
            new
            {
                operationId = Guid.NewGuid(),
                items = new[]
                {
                    new { productId = productA.Id, quantity = 3 },
                    new { productId = productB.Id, quantity = 3 }
                }
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetProductAsync(client, productA.Id))!.CurrentStock.Should().Be(10);
        (await GetProductAsync(client, productB.Id))!.CurrentStock.Should().Be(2);
    }

    [Fact]
    public async Task DuplicateProductLines_AreRejected()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 10);

        var response = await client.PostAsJsonAsync(
            "/api/v1/workorders",
            new
            {
                title = "Duplicate line order",
                description = "Invalid test order",
                priority = "High",
                items = new[]
                {
                    new { productId = product.Id, quantityRequested = 1 },
                    new { productId = product.Id, quantityRequested = 2 }
                }
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<WorkOrderDto> CreateDraftAsync(
        HttpClient client,
        Guid productId,
        int quantity) =>
        await CreateDraftAsync(client, new[] { new { productId, quantityRequested = quantity } });

    private static async Task<WorkOrderDto> CreateDraftAsync<T>(HttpClient client, T items)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/workorders",
            new
            {
                title = "Lifecycle test order",
                description = "Created by PostgreSQL integration tests",
                priority = "High",
                items
            },
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<WorkOrderDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
    }

    private static async Task SubmitApproveAndStartAsync(HttpClient client, Guid workOrderId)
    {
        (await client.PostAsync(
            $"/api/v1/workorders/{workOrderId}/submit",
            null,
            TestContext.Current.CancellationToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await client.GetFromJsonAsync<PaginatedResult<UserDto>>(
            "/api/v1/users?pageSize=50",
            JsonOptions,
            TestContext.Current.CancellationToken);
        var adminId = users!.Items.Single(user => user.Email == "admin@stockverity.local").Id;

        (await client.PostAsJsonAsync(
            $"/api/v1/workorders/{workOrderId}/approve",
            new { assignedToId = adminId },
            TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsync(
            $"/api/v1/workorders/{workOrderId}/start",
            null,
            TestContext.Current.CancellationToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

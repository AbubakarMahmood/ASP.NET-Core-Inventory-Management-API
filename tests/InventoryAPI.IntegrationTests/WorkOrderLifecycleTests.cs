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

    private async Task<ProductDto> CreateProductAsync(HttpClient client, int currentStock)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"WO-{Guid.NewGuid():N}"[..20],
            name = "Work order test product",
            description = "d",
            category = "Test",
            currentStock,
            reorderPoint = 1,
            reorderQuantity = 5,
            unitOfMeasure = "EA",
            unitCost = 3.00,
            location = "W-01-01"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions))!;
    }

    private async Task<WorkOrderDto> CreateDraftAsync(HttpClient client, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workorders", new
        {
            title = "Lifecycle test order",
            description = "Created by integration tests",
            priority = "High",
            items = new[] { new { productId, quantityRequested = quantity } }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOptions))!;
    }

    private async Task<Guid> GetAdminUserIdAsync(HttpClient client)
    {
        var users = await client.GetFromJsonAsync<PaginatedResult<UserDto>>(
            "/api/v1/users?pageSize=50", JsonOptions);
        return users!.Items.First(u => u.Email == "admin@inventory.com").Id;
    }

    [Fact]
    public async Task FullLifecycle_DraftToCompleted_WithStockIssued()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 50);
        var draft = await CreateDraftAsync(client, product.Id, quantity: 10);

        draft.Status.Should().Be(WorkOrderStatus.Draft);
        draft.OrderNumber.Should().MatchRegex(@"^WO-\d{8}-\d{4}$");
        draft.Items.Should().ContainSingle(i => i.ProductId == product.Id);
        draft.RequestedByEmail.Should().Be("admin@inventory.com");

        // Submit
        var submit = await client.PostAsync($"/api/v1/workorders/{draft.Id}/submit", null);
        submit.StatusCode.Should().Be(HttpStatusCode.OK);

        // Approve and assign
        var adminId = await GetAdminUserIdAsync(client);
        var approve = await client.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/approve", new
        {
            assignedToId = adminId
        });
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approve.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOptions);
        approved!.Status.Should().Be(WorkOrderStatus.Approved);
        approved.AssignedToEmail.Should().Be("admin@inventory.com");

        // Start
        var start = await client.PostAsync($"/api/v1/workorders/{draft.Id}/start", null);
        start.StatusCode.Should().Be(HttpStatusCode.OK);

        // Issue items — must decrement stock and track issued quantity
        var issue = await client.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/issue-items", new
        {
            items = new[] { new { productId = product.Id, quantity = 4 } }
        });
        issue.StatusCode.Should().Be(HttpStatusCode.OK);
        var issued = await issue.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOptions);
        issued!.Items[0].QuantityIssued.Should().Be(4);

        var updatedProduct = await client.GetFromJsonAsync<ProductDto>(
            $"/api/v1/products/{product.Id}", JsonOptions);
        updatedProduct!.CurrentStock.Should().Be(46);

        // Complete
        var complete = await client.PostAsync($"/api/v1/workorders/{draft.Id}/complete", null);
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await complete.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOptions);
        completed!.Status.Should().Be(WorkOrderStatus.Completed);
        completed.CompletedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Reject_RequiresReason_AndStoresIt()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 5);
        var draft = await CreateDraftAsync(client, product.Id, quantity: 2);

        await client.PostAsync($"/api/v1/workorders/{draft.Id}/submit", null);

        // Missing reason is rejected by validation
        var noReason = await client.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/reject", new
        {
            reason = ""
        });
        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var reject = await client.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/reject", new
        {
            reason = "Not needed this quarter"
        });
        reject.StatusCode.Should().Be(HttpStatusCode.OK);

        var rejected = await reject.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOptions);
        rejected!.Status.Should().Be(WorkOrderStatus.Rejected);
        rejected.RejectionReason.Should().Be("Not needed this quarter");
    }

    [Fact]
    public async Task IssueItems_MoreThanRequested_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 100);
        var draft = await CreateDraftAsync(client, product.Id, quantity: 5);

        await client.PostAsync($"/api/v1/workorders/{draft.Id}/submit", null);
        var adminId = await GetAdminUserIdAsync(client);
        await client.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/approve", new { assignedToId = adminId });
        await client.PostAsync($"/api/v1/workorders/{draft.Id}/start", null);

        var issue = await client.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/issue-items", new
        {
            items = new[] { new { productId = product.Id, quantity = 6 } }
        });

        issue.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_AlreadySubmittedOrder_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, currentStock: 5);
        var draft = await CreateDraftAsync(client, product.Id, quantity: 1);

        await client.PostAsync($"/api/v1/workorders/{draft.Id}/submit", null);
        var second = await client.PostAsync($"/api/v1/workorders/{draft.Id}/submit", null);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Approve_AsOperator_Returns403()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(admin, currentStock: 5);
        var draft = await CreateDraftAsync(admin, product.Id, quantity: 1);
        await admin.PostAsync($"/api/v1/workorders/{draft.Id}/submit", null);

        var operatorClient = await CreateAuthenticatedClientAsync("operator@inventory.com", "Operator123!");
        var approve = await operatorClient.PostAsJsonAsync($"/api/v1/workorders/{draft.Id}/approve", new
        {
            assignedToId = Guid.NewGuid()
        });

        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnknownWorkOrder_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync($"/api/v1/workorders/{Guid.NewGuid()}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

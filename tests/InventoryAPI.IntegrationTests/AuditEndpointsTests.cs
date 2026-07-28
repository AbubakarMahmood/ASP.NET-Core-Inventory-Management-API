using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public class AuditEndpointsTests : ApiTestBase
{
    public AuditEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DeletedProductsAndUsers_RemainVisibleInAuditHistory()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client, openingStock: 0);
        var productDelete = await client.DeleteAsync(
            $"/api/v1/products/{product.Id}",
            TestContext.Current.CancellationToken);
        productDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var userCreate = await client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                email = $"audit-{Guid.NewGuid():N}@example.com",
                password = "AuditUser123!",
                firstName = "Audit",
                lastName = "Subject",
                role = "Operator",
                isActive = true
            },
            TestContext.Current.CancellationToken);
        var userCreateBody = await userCreate.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        userCreate.StatusCode.Should().Be(HttpStatusCode.Created, userCreateBody);
        var user = (await userCreate.Content.ReadFromJsonAsync<UserDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;

        var userDelete = await client.DeleteAsync(
            $"/api/v1/users/{user.Id}",
            TestContext.Current.CancellationToken);
        userDelete.StatusCode.Should().Be(HttpStatusCode.OK);

        var deletedProducts = await client.GetFromJsonAsync<PaginatedResult<AuditLogDto>>(
            "/api/v1/audit?entityType=Product&action=Deleted&pageSize=50",
            JsonOptions,
            TestContext.Current.CancellationToken);
        var deletedUsers = await client.GetFromJsonAsync<PaginatedResult<AuditLogDto>>(
            "/api/v1/audit?entityType=User&action=Deleted&pageSize=50",
            JsonOptions,
            TestContext.Current.CancellationToken);

        deletedProducts!.Items.Should().Contain(entry => entry.EntityId == product.Id);
        deletedUsers!.Items.Should().Contain(entry => entry.EntityId == user.Id);
    }
}

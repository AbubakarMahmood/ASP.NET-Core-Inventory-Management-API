using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryAPI.Application.DTOs;

namespace InventoryAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Shares one real API host and PostgreSQL database across the tests in a class.
/// Test data uses unique identifiers so cases remain independent.
/// </summary>
public abstract class ApiTestBase : IClassFixture<TestWebApplicationFactory>
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected TestWebApplicationFactory Factory { get; }

    protected ApiTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected HttpClient CreateClient() => Factory.CreateClient();

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "admin@stockverity.local",
        string password = "Admin123!")
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Login returned no authentication payload.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return client;
    }

    protected static async Task<ProductDto> CreateProductAsync(
        HttpClient client,
        int openingStock = 10,
        string? sku = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                sku = sku ?? $"TEST-{Guid.NewGuid():N}"[..30],
                name = "Integration test product",
                description = "Created by the PostgreSQL integration suite",
                category = "Tests",
                openingStock,
                reorderPoint = 2,
                reorderQuantity = 10,
                unitOfMeasure = "EA",
                unitCost = 3.25m,
                location = "TEST-01"
            },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
    }

    protected static Task<ProductDto?> GetProductAsync(HttpClient client, Guid productId) =>
        client.GetFromJsonAsync<ProductDto>(
            $"/api/v1/products/{productId}",
            JsonOptions,
            TestContext.Current.CancellationToken);
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryAPI.Application.DTOs;

namespace InventoryAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Shares one API host (and one seeded database) across the tests in a class.
/// </summary>
public abstract class ApiTestBase : IClassFixture<TestWebApplicationFactory>
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    protected TestWebApplicationFactory Factory { get; }

    protected ApiTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>
    /// Returns a client authenticated as one of the seeded users.
    /// </summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "admin@inventory.com",
        string password = "Admin123!")
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }
}

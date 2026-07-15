using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.DTOs;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public class AuthEndpointsTests : ApiTestBase
{
    public AuthEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsTokenPair()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@inventory.com",
            password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.ExpiresIn.Should().BeGreaterThan(0);
        auth.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns400WithoutRevealingWhichFieldFailed()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@inventory.com",
            password = "WrongPassword1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Refresh_WithTokenFromLogin_IssuesNewPairAndRotates()
    {
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "manager@inventory.com",
            password = "Manager123!"
        });
        var first = await login.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = first!.RefreshToken
        });

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await refresh.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        second!.RefreshToken.Should().NotBe(first.RefreshToken, "refresh tokens must rotate");

        // The old token is single-use
        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = first.RefreshToken
        });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithGarbageToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "not-a-real-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "operator@inventory.com",
            password = "Operator123!"
        });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = auth.RefreshToken
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

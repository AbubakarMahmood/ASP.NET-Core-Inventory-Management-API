using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Infrastructure.Data;
using InventoryAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAPI.IntegrationTests;

public class AuthEndpointsTests : ApiTestBase
{
    public AuthEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsTokenPair_AndPersistsOnlyDigest()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "admin@stockverity.local",
                password = "Admin123!"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken);
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.ExpiresIn.Should().BeGreaterThan(0);
        auth.TokenType.Should().Be("Bearer");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.SingleAsync(
            item => item.Email == "admin@stockverity.local",
            TestContext.Current.CancellationToken);

        user.RefreshTokenHash.Should().NotBeNullOrWhiteSpace();
        user.RefreshTokenHash.Should().HaveLength(64);
        user.RefreshTokenHash.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task Login_WithUnknownOrWrongCredentials_UsesSameGenericFailure()
    {
        var client = CreateClient();

        var unknown = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = $"missing-{Guid.NewGuid():N}@example.com",
                password = "WrongPassword1"
            },
            TestContext.Current.CancellationToken);
        var wrong = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "admin@stockverity.local",
                password = "WrongPassword1"
            },
            TestContext.Current.CancellationToken);

        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var unknownBody = await unknown.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var wrongBody = await wrong.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        unknownBody.Should().Contain("Invalid email or password");
        wrongBody.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndPriorTokenCannotBeReused()
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "manager@stockverity.local",
                password = "Manager123!"
            },
            TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        var first = (await login.Content.ReadFromJsonAsync<AuthResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;

        var refresh = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = first.RefreshToken },
            TestContext.Current.CancellationToken);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = (await refresh.Content.ReadFromJsonAsync<AuthResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken))!;
        second.RefreshToken.Should().NotBe(first.RefreshToken);

        var replay = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = first.RefreshToken },
            TestContext.Current.CancellationToken);
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedSurfaces_EnforceAuthenticationAndRoles()
    {
        var anonymous = CreateClient();
        var anonymousProducts = await anonymous.GetAsync(
            "/api/v1/products",
            TestContext.Current.CancellationToken);
        anonymousProducts.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var operatorClient = await CreateAuthenticatedClientAsync(
            "operator@stockverity.local",
            "Operator123!");
        var forbiddenCreate = await operatorClient.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                sku = $"FORBIDDEN-{Guid.NewGuid():N}"[..30],
                name = "Forbidden product",
                description = "Role enforcement test",
                category = "Tests",
                openingStock = 0,
                reorderPoint = 1,
                reorderQuantity = 1,
                unitOfMeasure = "EA",
                unitCost = 1m,
                location = "TEST-01"
            },
            TestContext.Current.CancellationToken);
        forbiddenCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var anonymousNegotiate = await anonymous.PostAsync(
            "/api/v1/notifications/negotiate?negotiateVersion=1",
            null,
            TestContext.Current.CancellationToken);
        anonymousNegotiate.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var authenticatedNegotiate = await operatorClient.PostAsync(
            "/api/v1/notifications/negotiate?negotiateVersion=1",
            null,
            TestContext.Current.CancellationToken);
        authenticatedNegotiate.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithLedgerHistory_CannotBeDeleted()
    {
        var client = await CreateAuthenticatedClientAsync();
        await CreateProductAsync(client, openingStock: 1);

        Guid adminId;
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            adminId = await context.Users
                .Where(user => user.Email == "admin@stockverity.local")
                .Select(user => user.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.DeleteAsync(
            $"/api/v1/users/{adminId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

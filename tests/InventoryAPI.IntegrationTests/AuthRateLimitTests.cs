using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.IntegrationTests.Infrastructure;

namespace InventoryAPI.IntegrationTests;

public sealed class AuthRateLimitTests
{
    [Fact]
    public async Task LoginLimiter_RejectsTheEleventhRequestFromOneClient()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 11; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new
                {
                    email = $"missing-{attempt}@example.com",
                    password = "WrongPassword1"
                },
                TestContext.Current.CancellationToken);
            statuses.Add(response.StatusCode);
        }

        statuses.Take(10).Should().OnlyContain(status => status == HttpStatusCode.BadRequest);
        statuses[10].Should().Be(HttpStatusCode.TooManyRequests);
    }
}

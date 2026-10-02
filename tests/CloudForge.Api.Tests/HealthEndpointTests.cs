using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CloudForge.Api.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        await using var app = new WebApplicationFactory<Program>();

        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(result);
        Assert.Equal("Healthy", result.Status);
        Assert.Equal("CloudForge API", result.Service);
    }

    private sealed record HealthResponse(
        string Status,
        string Service
    );
}

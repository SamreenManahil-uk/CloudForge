using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CloudForge.Api.Tests.Infrastructure;
using Xunit;

namespace CloudForge.Api.Tests;

public class AuthorizationTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.Created)]
    [InlineData("Admin", HttpStatusCode.Created)]
    public async Task CreateApplication_EnforcesRoles(
        string? role,
        HttpStatusCode expectedStatus)
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        if (role is not null)
            TestAuthentication.SignIn(client, factory, role);

        using var response = await client.PostAsJsonAsync(
            "/api/applications/",
            new
            {
                name = $"RBAC Test {Guid.NewGuid():N}",
                repositoryUrl =
                    "https://github.com/example/rbac-test",
                environment = "development"
            });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.Created)]
    [InlineData("Admin", HttpStatusCode.Created)]
    public async Task CreateDeployment_EnforcesRoles(
        string? role,
        HttpStatusCode expectedStatus)
    {
        await using var factory = new CloudForgeTestFactory();

        // Set up an existing application using an authorized user.
        using var setupClient = factory.CreateClient();
        TestAuthentication.SignIn(
            setupClient,
            factory,
            "Developer");

        using var applicationResponse =
            await setupClient.PostAsJsonAsync(
                "/api/applications/",
                new
                {
                    name = $"Deployment RBAC {Guid.NewGuid():N}",
                    repositoryUrl =
                        "https://github.com/example/rbac-deployment",
                    environment = "development"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            applicationResponse.StatusCode);

        using var applicationJson = JsonDocument.Parse(
            await applicationResponse.Content.ReadAsStringAsync());

        var applicationId = applicationJson.RootElement
            .GetProperty("id")
            .GetGuid();

        // Use a separate client for the role being tested.
        using var client = factory.CreateClient();

        if (role is not null)
            TestAuthentication.SignIn(client, factory, role);

        using var response = await client.PostAsJsonAsync(
            "/api/deployments/",
            new
            {
                applicationId,
                environment = "staging",
                version = "1.0.0"
            });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/applications/")]
    [InlineData("/api/deployments/")]
    public async Task ReadEndpoints_RemainPublicForNow(
        string endpoint)
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CloudForge.Api.Tests.Infrastructure;
using Xunit;

namespace CloudForge.Api.Tests;

public class DeploymentEndpointsTests
{
    private static async Task<Guid> CreateApplication(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/applications/",
            new
            {
                name = $"Deployment Test {Guid.NewGuid():N}",
                repositoryUrl = "https://github.com/example/deployment-test",
                environment = "development"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task CreateDeployment_ReturnsPendingDeployment()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        var applicationId = await CreateApplication(client);

        using var response = await client.PostAsJsonAsync(
            "/api/deployments/",
            new
            {
                applicationId,
                environment = "staging",
                version = "1.0.0"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal("pending",
            json.RootElement.GetProperty("status").GetString());

        Assert.Equal(applicationId,
            json.RootElement.GetProperty("applicationId").GetGuid());

        Assert.NotEqual(Guid.Empty,
            json.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetDeployment_ReturnsCreatedDeployment()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        var applicationId = await CreateApplication(client);

        using var created = await client.PostAsJsonAsync(
            "/api/deployments/",
            new
            {
                applicationId,
                environment = "development",
                version = "2.0.0"
            });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var createdJson = JsonDocument.Parse(
            await created.Content.ReadAsStringAsync());

        var deploymentId = createdJson.RootElement
            .GetProperty("id").GetGuid();

        using var response = await client.GetAsync(
            $"/api/deployments/{deploymentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal("2.0.0",
            json.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task GetDeployments_ReturnsArray()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        using var response = await client.GetAsync(
            "/api/deployments/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(JsonValueKind.Array,
            json.RootElement.ValueKind);
    }

    [Fact]
    public async Task CreateDeployment_RejectsInvalidEnvironment()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        var applicationId = await CreateApplication(client);

        using var response = await client.PostAsJsonAsync(
            "/api/deployments/",
            new
            {
                applicationId,
                environment = "invalid-environment",
                version = "1.0.0"
            });

        Assert.Equal(HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateDeployment_RejectsUnknownApplication()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        using var response = await client.PostAsJsonAsync(
            "/api/deployments/",
            new
            {
                applicationId = Guid.NewGuid(),
                environment = "staging",
                version = "1.0.0"
            });

        Assert.Equal(HttpStatusCode.NotFound,
            response.StatusCode);
    }
}

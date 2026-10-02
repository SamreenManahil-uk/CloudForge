using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CloudForge.Api.Tests.Infrastructure;
using Xunit;

namespace CloudForge.Api.Tests;

public class ApplicationEndpointsTests
{
    [Fact]
    public async Task GetApplications_ReturnsSuccessfulResponse()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        using var response =
            await client.GetAsync("/api/applications/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
    }

    [Fact]
    public async Task RegisterApplication_ReturnsCreatedApplication()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        var name = $"CloudForge Test {Guid.NewGuid():N}";

        var request = new
        {
            name,
            repositoryUrl = "https://github.com/example/cloudforge-test",
            environment = "development"
        };

        using var response = await client.PostAsJsonAsync(
            "/api/applications/",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            name,
            json.RootElement.GetProperty("name").GetString());

        Assert.NotEqual(
            Guid.Empty,
            json.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task RegisterApplication_RejectsInvalidRepositoryUrl()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        var request = new
        {
            name = "Invalid Repository Test",
            repositoryUrl = "http://example.com/insecure",
            environment = "development"
        };

        using var response = await client.PostAsJsonAsync(
            "/api/applications/",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetApplicationById_ReturnsRegisteredApplication()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        var name = $"Application Lookup {Guid.NewGuid():N}";

        using var created = await client.PostAsJsonAsync(
            "/api/applications/",
            new
            {
                name,
                repositoryUrl = "https://github.com/example/lookup",
                environment = "staging"
            });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var createdJson = JsonDocument.Parse(
            await created.Content.ReadAsStringAsync());

        var id = createdJson.RootElement
            .GetProperty("id")
            .GetGuid();

        using var response = await client.GetAsync(
            $"/api/applications/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            name,
            json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetUnknownApplication_ReturnsNotFound()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();
        TestAuthentication.SignIn(client, factory);

        using var response = await client.GetAsync(
            $"/api/applications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

using System.Net;
using CloudForge.Api.Tests.Infrastructure;
using Xunit;

namespace CloudForge.Api.Tests;

public class AdminEndpointsTests
{
    [Fact]
    public async Task AnonymousUser_CannotListUsers()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        using var response =
            await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("Developer")]
    public async Task NonAdmin_CannotListUsers(string role)
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        TestAuthentication.SignIn(client, factory, role);

        using var response =
            await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}

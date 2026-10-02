using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using CloudForge.Api.Data;
using CloudForge.Api.Security;
using CloudForge.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CloudForge.Api.Tests;

public class AdminRoleTests
{
    private static UserEntity CreateUser(
        CloudForgeTestFactory factory,
        string role)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CloudForgeDbContext>();

        var user = new UserEntity
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            Role = role,
            PasswordHash = "test-only"
        };

        db.Users.Add(user);
        db.SaveChanges();

        return user;
    }

    private static void SignIn(
        HttpClient client,
        CloudForgeTestFactory factory,
        UserEntity user)
    {
        using var scope = factory.Services.CreateScope();

        var tokens = scope.ServiceProvider
            .GetRequiredService<JwtTokenService>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokens.GenerateToken(user));
    }

    [Fact]
    public async Task Admin_CanListUsers()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        var admin = CreateUser(factory, "Admin");
        SignIn(client, factory, admin);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains(admin.Email, content);
        Assert.DoesNotContain("passwordHash", content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_CanPromoteViewer()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        var admin = CreateUser(factory, "Admin");
        var viewer = CreateUser(factory, "Viewer");

        SignIn(client, factory, admin);

        var response = await client.PatchAsJsonAsync(
            $"/api/admin/users/{viewer.Id}/role",
            new { role = "Developer" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CloudForgeDbContext>();

        Assert.Equal(
            "Developer",
            db.Users.Single(u => u.Id == viewer.Id).Role);
    }

    [Fact]
    public async Task Admin_CannotChangeOwnRole()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        var admin = CreateUser(factory, "Admin");
        SignIn(client, factory, admin);

        var response = await client.PatchAsJsonAsync(
            $"/api/admin/users/{admin.Id}/role",
            new { role = "Viewer" });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotAssignInvalidRole()
    {
        await using var factory = new CloudForgeTestFactory();
        using var client = factory.CreateClient();

        var admin = CreateUser(factory, "Admin");
        var viewer = CreateUser(factory, "Viewer");

        SignIn(client, factory, admin);

        var response = await client.PatchAsJsonAsync(
            $"/api/admin/users/{viewer.Id}/role",
            new { role = "SuperAdmin" });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}

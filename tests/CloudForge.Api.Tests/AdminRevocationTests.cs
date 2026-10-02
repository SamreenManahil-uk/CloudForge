using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudForge.Api.Data;
using CloudForge.Api.Security;
using CloudForge.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CloudForge.Api.Tests;

public class AdminRevocationTests
{
    [Fact]
    public async Task DemotedAdmin_CannotReuseOldJwt()
    {
        await using var factory = new CloudForgeTestFactory();

        using var oldAdminClient = factory.CreateClient();
        using var activeAdminClient = factory.CreateClient();

        UserEntity oldAdmin;
        UserEntity activeAdmin;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<CloudForgeDbContext>();

            oldAdmin = new UserEntity
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                Role = "Admin",
                PasswordHash = "test-only"
            };

            activeAdmin = new UserEntity
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                Role = "Admin",
                PasswordHash = "test-only"
            };

            db.Users.AddRange(oldAdmin, activeAdmin);
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider
                .GetRequiredService<JwtTokenService>();

            oldAdminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    tokens.GenerateToken(oldAdmin));

            activeAdminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    tokens.GenerateToken(activeAdmin));
        }

        var before = await oldAdminClient.GetAsync(
            "/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        var demotion = await activeAdminClient.PatchAsJsonAsync(
            $"/api/admin/users/{oldAdmin.Id}/role",
            new { role = "Viewer" });

        Assert.Equal(HttpStatusCode.OK, demotion.StatusCode);

        var after = await oldAdminClient.GetAsync(
            "/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            after.StatusCode);
    }
}

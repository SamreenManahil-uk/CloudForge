using System.Net.Http.Headers;
using CloudForge.Api.Data;
using CloudForge.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CloudForge.Api.Tests.Infrastructure;

public static class TestAuthentication
{
    public static void SignIn(
        HttpClient client,
        CloudForgeTestFactory factory,
        string role = "Developer")
    {
        using var scope = factory.Services.CreateScope();

        var tokens = scope.ServiceProvider
            .GetRequiredService<JwtTokenService>();

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Role = role
        };

        var token = tokens.GenerateToken(user);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}

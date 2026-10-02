using Microsoft.Extensions.Configuration;
using CloudForge.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CloudForge.Api.Tests.Infrastructure;

public sealed class CloudForgeTestFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    public CloudForgeTestFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Issuer", "CloudForge.Tests");
        builder.UseSetting("Jwt:Audience", "CloudForge.Tests");
        builder.UseSetting(
            "Jwt:Secret",
            "CloudForgeTestOnlySecretKey12345678901234567890");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] =
                        "CloudForgeTestOnlySecretKey12345678901234567890",
                    ["Jwt:Issuer"] = "CloudForge.Tests",
                    ["Jwt:Audience"] = "CloudForge.Tests"
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CloudForgeDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CloudForgeDbContext>>();

            services.AddDbContext<CloudForgeDbContext>(
                options => options.UseSqlite(_connection));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var database = scope.ServiceProvider
                .GetRequiredService<CloudForgeDbContext>();

            database.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

using CloudForge.Api.Data;
using CloudForge.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CloudForge.Api.Tests;

public class DeploymentStatusServiceTests
{
    private static async Task<(
        SqliteConnection Connection,
        DbContextOptions<CloudForgeDbContext> Options,
        Guid DeploymentId)> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<CloudForgeDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new CloudForgeDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var application = new ApplicationEntity
        {
            Name = "CloudForge Test Application",
            RepositoryUrl =
                "https://github.com/example/test",
            Environment = "development"
        };

        db.Applications.Add(application);

        var deployment = new DeploymentEntity
        {
            Application = application,
            Environment = "development",
            Version = "1.0.0",
            Status = DeploymentLifecycle.Pending
        };

        db.Deployments.Add(deployment);

        await db.SaveChangesAsync();

        return (connection, options, deployment.Id);
    }

    private static DeploymentStatusService CreateService(
        CloudForgeDbContext db)
    {
        return new DeploymentStatusService(
            db,
            NullLogger<DeploymentStatusService>.Instance);
    }

    [Fact]
    public async Task PendingDeployment_CanBecomeRunning()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var updated = await CreateService(db)
            .TryTransitionAsync(
                setup.DeploymentId,
                "pending",
                "running");

        var deployment = await db.Deployments
            .AsNoTracking()
            .SingleAsync(d => d.Id == setup.DeploymentId);

        Assert.True(updated);
        Assert.Equal("running", deployment.Status);
        Assert.Null(deployment.CompletedAt);
    }

    [Fact]
    public async Task Deployment_CannotBeClaimedTwice()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var service = CreateService(db);

        var first = await service.TryTransitionAsync(
            setup.DeploymentId, "pending", "running");

        var second = await service.TryTransitionAsync(
            setup.DeploymentId, "pending", "running");

        Assert.True(first);
        Assert.False(second);
    }

    [Theory]
    [InlineData("succeeded")]
    [InlineData("failed")]
    public async Task RunningDeployment_CanFinish(
        string finalStatus)
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var service = CreateService(db);

        Assert.True(await service.TryTransitionAsync(
            setup.DeploymentId, "pending", "running"));

        Assert.True(await service.TryTransitionAsync(
            setup.DeploymentId, "running", finalStatus));

        var deployment = await db.Deployments
            .AsNoTracking()
            .SingleAsync(d => d.Id == setup.DeploymentId);

        Assert.Equal(finalStatus, deployment.Status);
        Assert.NotNull(deployment.CompletedAt);
    }

    [Fact]
    public async Task InvalidTransition_DoesNotModifyDatabase()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TryTransitionAsync(
                setup.DeploymentId,
                "pending",
                "succeeded"));

        var deployment = await db.Deployments
            .AsNoTracking()
            .SingleAsync(d => d.Id == setup.DeploymentId);

        Assert.Equal("pending", deployment.Status);
        Assert.Null(deployment.CompletedAt);
    }
}

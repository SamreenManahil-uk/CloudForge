using CloudForge.Api.Data;
using CloudForge.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CloudForge.Api.Tests;

public sealed class DeploymentClaimServiceTests
{
    private static async Task<(
        SqliteConnection Connection,
        DbContextOptions<CloudForgeDbContext> Options)>
        CreateDatabaseAsync()
    {
        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<CloudForgeDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new CloudForgeDbContext(options);

        await db.Database.EnsureCreatedAsync();

        return (connection, options);
    }

    private static DeploymentClaimService CreateService(
        CloudForgeDbContext db)
    {
        var statusService = new DeploymentStatusService(
            db,
            NullLogger<DeploymentStatusService>.Instance);

        return new DeploymentClaimService(
            db,
            statusService,
            NullLogger<DeploymentClaimService>.Instance);
    }

    private static async Task<Guid> AddDeploymentAsync(
        CloudForgeDbContext db,
        string status = "pending",
        DateTimeOffset? createdAt = null)
    {
        var application = new ApplicationEntity
        {
            Name = "Claim Test Application",
            RepositoryUrl =
                "https://github.com/example/claim-test",
            Environment = "development"
        };

        var deployment = new DeploymentEntity
        {
            Application = application,
            Environment = "development",
            Version = "1.0.0",
            Status = status,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };

        db.Deployments.Add(deployment);
        await db.SaveChangesAsync();

        return deployment.Id;
    }

    [Fact]
    public async Task EmptyQueue_ReturnsNull()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var result =
            await CreateService(db).TryClaimNextAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task PendingDeployment_IsClaimed()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var deploymentId =
            await AddDeploymentAsync(db);

        var result =
            await CreateService(db).TryClaimNextAsync();

        var stored = await db.Deployments
            .AsNoTracking()
            .SingleAsync(d => d.Id == deploymentId);

        Assert.Equal(deploymentId, result);
        Assert.Equal("running", stored.Status);
        Assert.Null(stored.CompletedAt);
    }

    [Fact]
    public async Task SameDeployment_CannotBeClaimedTwice()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var deploymentId =
            await AddDeploymentAsync(db);

        var service = CreateService(db);

        var first = await service.TryClaimNextAsync();
        var second = await service.TryClaimNextAsync();

        Assert.Equal(deploymentId, first);
        Assert.Null(second);
    }

    [Fact]
    public async Task OldestPendingDeployment_IsClaimedFirst()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        var now = DateTimeOffset.UtcNow;

        var newerId = await AddDeploymentAsync(
            db, createdAt: now);

        var olderId = await AddDeploymentAsync(
            db, createdAt: now.AddMinutes(-10));

        var service = CreateService(db);

        var first = await service.TryClaimNextAsync();
        var second = await service.TryClaimNextAsync();

        Assert.Equal(olderId, first);
        Assert.Equal(newerId, second);
    }

    [Fact]
    public async Task RunningDeployment_IsSkipped()
    {
        var setup = await CreateDatabaseAsync();
        await using var connection = setup.Connection;
        await using var db =
            new CloudForgeDbContext(setup.Options);

        await AddDeploymentAsync(db, "running");

        var pendingId =
            await AddDeploymentAsync(db, "pending");

        var result =
            await CreateService(db).TryClaimNextAsync();

        Assert.Equal(pendingId, result);
    }
}

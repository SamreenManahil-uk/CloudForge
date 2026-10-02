using CloudForge.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Endpoints;

public static class DeploymentEndpoints
{
    public record CreateDeploymentRequest(
        Guid ApplicationId,
        string Environment,
        string Version);

    public record DeploymentResponse(
        Guid Id,
        Guid ApplicationId,
        string Environment,
        string Version,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt);

    public static IEndpointRouteBuilder MapDeploymentEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/deployments");

        group.MapGet("/", async (
            CloudForgeDbContext db,
            CancellationToken ct) =>
        {
            var deployments = await db.Deployments
                .AsNoTracking()
                .Select(d => new DeploymentResponse(
                    d.Id,
                    d.ApplicationId,
                    d.Environment,
                    d.Version,
                    d.Status,
                    d.CreatedAt,
                    d.CompletedAt))
                .ToListAsync(ct);

            deployments = deployments
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return Results.Ok(deployments);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            CloudForgeDbContext db,
            CancellationToken ct) =>
        {
            var deployment = await db.Deployments
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => new DeploymentResponse(
                    d.Id,
                    d.ApplicationId,
                    d.Environment,
                    d.Version,
                    d.Status,
                    d.CreatedAt,
                    d.CompletedAt))
                .FirstOrDefaultAsync(ct);

            return deployment is null
                ? Results.NotFound()
                : Results.Ok(deployment);
        });

        group.MapPost("/", async (
            CreateDeploymentRequest request,
            CloudForgeDbContext db,
            CancellationToken ct) =>
        {
            if (request.ApplicationId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Environment) ||
                string.IsNullOrWhiteSpace(request.Version))
            {
                return Results.BadRequest(
                    new { error = "Valid application, environment and version are required." });
            }

            var environment = request.Environment.Trim().ToLowerInvariant();
            var version = request.Version.Trim();

            if (environment.Length > 50 || version.Length > 100)
            {
                return Results.BadRequest(
                    new { error = "Environment or version exceeds maximum length." });
            }

            if (environment is not ("development" or "staging" or "production"))
            {
                return Results.BadRequest(
                    new { error = "Environment must be development, staging or production." });
            }

            var applicationExists = await db.Applications
                .AnyAsync(a => a.Id == request.ApplicationId, ct);

            if (!applicationExists)
            {
                return Results.NotFound(
                    new { error = "Application not found." });
            }

            var deployment = new DeploymentEntity
            {
                Id = Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                Environment = environment,
                Version = version,
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Deployments.Add(deployment);
            await db.SaveChangesAsync(ct);

            var response = new DeploymentResponse(
                deployment.Id,
                deployment.ApplicationId,
                deployment.Environment,
                deployment.Version,
                deployment.Status,
                deployment.CreatedAt,
                deployment.CompletedAt);

            return Results.Created(
                $"/api/deployments/{deployment.Id}",
                response);
        }).RequireAuthorization("CanDeploy");

        return app;
    }
}

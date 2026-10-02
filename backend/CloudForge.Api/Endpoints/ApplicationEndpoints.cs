using CloudForge.Api.Data;
using CloudForge.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/applications");

        group.MapGet("/", async (
            CloudForgeDbContext db,
            CancellationToken cancellationToken) =>
        {
            var applications = await db.Applications
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new DeveloperApplication(
                    x.Id,
                    x.Name,
                    x.RepositoryUrl,
                    x.Environment,
                    x.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(applications);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            CloudForgeDbContext db,
            CancellationToken cancellationToken) =>
        {
            var application = await db.Applications
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new DeveloperApplication(
                    x.Id,
                    x.Name,
                    x.RepositoryUrl,
                    x.Environment,
                    x.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);

            return application is null
                ? Results.NotFound()
                : Results.Ok(application);
        });

        group.MapPost("/", async (
            CreateApplicationRequest request,
            CloudForgeDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.RepositoryUrl) ||
                string.IsNullOrWhiteSpace(request.Environment))
            {
                return Results.BadRequest(
                    new { error = "All fields are required." });
            }

            var repositoryUrl = request.RepositoryUrl.Trim();

            if (!Uri.TryCreate(
                    repositoryUrl,
                    UriKind.Absolute,
                    out var repositoryUri) ||
                repositoryUri.Scheme != Uri.UriSchemeHttps)
            {
                return Results.BadRequest(
                    new { error = "A valid HTTPS repository URL is required." });
            }

            var entity = new ApplicationEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                RepositoryUrl = repositoryUrl,
                Environment = request.Environment.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Applications.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            var application = new DeveloperApplication(
                entity.Id,
                entity.Name,
                entity.RepositoryUrl,
                entity.Environment,
                entity.CreatedAt);

            return Results.Created(
                $"/api/applications/{application.Id}",
                application);
        }).RequireAuthorization("CanDeploy");

        return app;
    }
}

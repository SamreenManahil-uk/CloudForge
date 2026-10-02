using CloudForge.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Services;

public sealed class DeploymentClaimService(
    CloudForgeDbContext db,
    DeploymentStatusService statusService,
    ILogger<DeploymentClaimService> logger)
{
    public async Task<Guid?> TryClaimNextAsync(
        CancellationToken cancellationToken = default)
    {
        // Read a small batch because another worker might
        // claim the first deployment before we update it.
        var pending = db.Deployments
            .AsNoTracking()
            .Where(d => d.Status == DeploymentLifecycle.Pending);

        // SQLite cannot ORDER BY DateTimeOffset.
        // PostgreSQL supports server-side ordering.
        List<Guid> candidates;

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var rows = await pending
                .Select(d => new { d.Id, d.CreatedAt })
                .ToListAsync(cancellationToken);

            candidates = rows
                .OrderBy(d => d.CreatedAt)
                .ThenBy(d => d.Id)
                .Take(10)
                .Select(d => d.Id)
                .ToList();
        }
        else
        {
            candidates = await pending
                .OrderBy(d => d.CreatedAt)
                .ThenBy(d => d.Id)
                .Select(d => d.Id)
                .Take(10)
                .ToListAsync(cancellationToken);
        }

        foreach (var deploymentId in candidates)
        {
            var claimed = await statusService.TryTransitionAsync(
                deploymentId,
                DeploymentLifecycle.Pending,
                DeploymentLifecycle.Running,
                cancellationToken);

            if (!claimed)
                continue;

            logger.LogInformation(
                "Deployment {DeploymentId} claimed.",
                deploymentId);

            return deploymentId;
        }

        return null;
    }
}

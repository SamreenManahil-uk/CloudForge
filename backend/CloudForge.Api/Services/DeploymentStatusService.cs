using CloudForge.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Services;

public sealed class DeploymentStatusService(
    CloudForgeDbContext db,
    ILogger<DeploymentStatusService> logger)
{
    public async Task<bool> TryTransitionAsync(
        Guid deploymentId,
        string expectedStatus,
        string nextStatus,
        CancellationToken cancellationToken = default)
    {
        DeploymentLifecycle.ValidateTransition(
            expectedStatus,
            nextStatus);

        var isTerminal =
            DeploymentLifecycle.IsTerminal(nextStatus);

        var completedAt = isTerminal
            ? DateTimeOffset.UtcNow
            : (DateTimeOffset?)null;

        var updatedRows = await db.Deployments
            .Where(d =>
                d.Id == deploymentId &&
                d.Status == expectedStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        d => d.Status,
                        nextStatus)
                    .SetProperty(
                        d => d.CompletedAt,
                        completedAt),
                cancellationToken);

        if (updatedRows == 0)
        {
            logger.LogWarning(
                "Deployment {DeploymentId} transition " +
                "{ExpectedStatus} -> {NextStatus} was not applied.",
                deploymentId,
                expectedStatus,
                nextStatus);

            return false;
        }

        logger.LogInformation(
            "Deployment {DeploymentId}: {Previous} -> {Next}",
            deploymentId,
            expectedStatus,
            nextStatus);

        return true;
    }
}

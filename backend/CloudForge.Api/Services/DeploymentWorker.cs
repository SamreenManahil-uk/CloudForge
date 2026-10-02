using CloudForge.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Services;

public sealed class DeploymentWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DeploymentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Deployment worker started.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(3));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<CloudForgeDbContext>();

                var claimingEnabled =
                    configuration.GetValue<bool>(
                        "DeploymentWorker:EnableClaiming");

                if (!claimingEnabled)
                    continue;

                var claimService = scope.ServiceProvider
                    .GetRequiredService<DeploymentClaimService>();

                var statusService = scope.ServiceProvider
                    .GetRequiredService<DeploymentStatusService>();

                var deploymentId =
                    await claimService.TryClaimNextAsync(stoppingToken);

                if (deploymentId is null)
                    continue;

                logger.LogInformation(
                    "Simulating deployment {DeploymentId}",
                    deploymentId);

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);

                await statusService.TryTransitionAsync(
                    deploymentId.Value,
                    DeploymentLifecycle.Running,
                    DeploymentLifecycle.Succeeded,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Deployment worker failed.");
            }
        }
    }
}

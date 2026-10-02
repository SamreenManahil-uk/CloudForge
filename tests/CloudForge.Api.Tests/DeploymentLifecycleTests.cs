using CloudForge.Api.Services;
using Xunit;

namespace CloudForge.Api.Tests;

public class DeploymentLifecycleTests
{
    [Theory]
    [InlineData("pending", "running")]
    [InlineData("running", "succeeded")]
    [InlineData("running", "failed")]
    public void AllowsValidTransitions(
        string current,
        string next)
    {
        Assert.True(
            DeploymentLifecycle.CanTransition(current, next));

        DeploymentLifecycle.ValidateTransition(current, next);
    }

    [Theory]
    [InlineData("pending", "succeeded")]
    [InlineData("pending", "failed")]
    [InlineData("running", "pending")]
    [InlineData("succeeded", "running")]
    [InlineData("failed", "running")]
    [InlineData("pending", "pending")]
    public void RejectsInvalidTransitions(
        string current,
        string next)
    {
        Assert.False(
            DeploymentLifecycle.CanTransition(current, next));

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentLifecycle.ValidateTransition(current, next));
    }

    [Theory]
    [InlineData("succeeded", true)]
    [InlineData("failed", true)]
    [InlineData("pending", false)]
    [InlineData("running", false)]
    public void IdentifiesTerminalStatuses(
        string status,
        bool expected)
    {
        Assert.Equal(
            expected,
            DeploymentLifecycle.IsTerminal(status));
    }
}

namespace CloudForge.Api.Services;

public static class DeploymentLifecycle
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static bool CanTransition(
        string currentStatus,
        string nextStatus)
    {
        return (currentStatus, nextStatus) switch
        {
            (Pending, Running) => true,
            (Running, Succeeded) => true,
            (Running, Failed) => true,
            _ => false
        };
    }

    public static bool IsTerminal(string status)
    {
        return status is Succeeded or Failed;
    }

    public static void ValidateTransition(
        string currentStatus,
        string nextStatus)
    {
        if (!CanTransition(currentStatus, nextStatus))
        {
            throw new InvalidOperationException(
                $"Invalid deployment transition: " +
                $"{currentStatus} -> {nextStatus}");
        }
    }
}

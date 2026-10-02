namespace CloudForge.Api.Data;

public sealed class DeploymentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ApplicationId { get; set; }

    public ApplicationEntity Application { get; set; } = null!;

    public string Environment { get; set; } = "development";

    public string Version { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}

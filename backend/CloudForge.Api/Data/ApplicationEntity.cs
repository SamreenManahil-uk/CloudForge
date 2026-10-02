namespace CloudForge.Api.Data;

public sealed class ApplicationEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RepositoryUrl { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

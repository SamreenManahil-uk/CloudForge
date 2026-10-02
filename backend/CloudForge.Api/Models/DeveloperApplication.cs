namespace CloudForge.Api.Models;

public sealed record DeveloperApplication(
    Guid Id,
    string Name,
    string RepositoryUrl,
    string Environment,
    DateTimeOffset CreatedAt
);

public sealed record CreateApplicationRequest(
    string Name,
    string RepositoryUrl,
    string Environment
);

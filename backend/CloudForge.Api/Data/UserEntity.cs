namespace CloudForge.Api.Data;

public sealed class UserEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Viewer";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

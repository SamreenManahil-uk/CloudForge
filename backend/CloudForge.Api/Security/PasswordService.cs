using CloudForge.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace CloudForge.Api.Security;

public sealed class PasswordService
{
    private readonly PasswordHasher<UserEntity> _hasher = new();

    public string HashPassword(UserEntity user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(
        UserEntity user,
        string password)
    {
        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}

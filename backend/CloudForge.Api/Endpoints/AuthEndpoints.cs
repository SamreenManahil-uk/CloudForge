using CloudForge.Api.Data;
using CloudForge.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Endpoints;

public static class AuthEndpoints
{
    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request,
            CloudForgeDbContext db,
            PasswordService passwords,
            CancellationToken ct) =>
        {
            var email = request.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) ||
                email.Length > 254 ||
                !System.Net.Mail.MailAddress.TryCreate(
                    email, out var address) ||
                address.Address != email ||
                string.IsNullOrWhiteSpace(request.Password) ||
                request.Password.Length < 12 ||
                request.Password.Length > 128)
            {
                return Results.BadRequest(new
                {
                    error = "Valid email and password of 12-128 characters required."
                });
            }

            if (await db.Users.AnyAsync(
                u => u.Email == email, ct))
            {
                return Results.Conflict(new
                {
                    error = "Registration could not be completed."
                });
            }

            var user = new UserEntity
            {
                Email = email,
                Role = "Viewer"
            };

            user.PasswordHash =
                passwords.HashPassword(user, request.Password);

            db.Users.Add(user);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new
                {
                    error = "Registration could not be completed."
                });
            }

            return Results.Created(
                $"/api/auth/users/{user.Id}",
                new
                {
                    user.Id,
                    user.Email,
                    user.Role
                });
        }).AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            CloudForgeDbContext db,
            PasswordService passwords,
            JwtTokenService tokens,
            CancellationToken ct) =>
        {
            var email = request.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(request.Password))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users
                .SingleOrDefaultAsync(
                    u => u.Email == email, ct);

            if (user is null ||
                !passwords.VerifyPassword(
                    user, request.Password))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                accessToken = tokens.GenerateToken(user),
                tokenType = "Bearer",
                expiresIn = 1800,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Role
                }
            });
        }).AllowAnonymous();
    }
}

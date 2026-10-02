using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloudForge.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Endpoints;

public static class AdminEndpoints
{
    public record UpdateRoleRequest(string Role);

    public static IEndpointRouteBuilder MapAdminEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .RequireAuthorization("AdminOnly")
            .WithTags("Administration")
            .AddEndpointFilter(async (context, next) =>
            {
                var principal = context.HttpContext.User;

                var id = principal.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                id ??= principal.FindFirstValue(
                    JwtRegisteredClaimNames.Sub);

                if (!Guid.TryParse(id, out var userId))
                    return Results.Forbid();

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<CloudForgeDbContext>();

                var isCurrentAdmin = await db.Users
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.Id == userId &&
                        u.Role == "Admin",
                        context.HttpContext.RequestAborted);

                if (!isCurrentAdmin)
                    return Results.Forbid();

                return await next(context);
            });

        group.MapGet("/users", async (
            CloudForgeDbContext db,
            CancellationToken ct) =>
        {
            var users = await db.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync(ct);

            return Results.Ok(users);
        });

        group.MapPatch("/users/{id:guid}/role", async (
            Guid id,
            UpdateRoleRequest request,
            ClaimsPrincipal principal,
            CloudForgeDbContext db,
            CancellationToken ct) =>
        {
            var requestedRole = request.Role?.Trim();

            if (requestedRole is not
                ("Viewer" or "Developer" or "Admin"))
            {
                return Results.BadRequest(new
                {
                    error = "Role must be Viewer, Developer or Admin."
                });
            }

            var currentUserId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            currentUserId ??= principal.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

            if (Guid.TryParse(currentUserId, out var adminId)
                && adminId == id)
            {
                return Results.BadRequest(new
                {
                    error = "You cannot change your own role."
                });
            }

            var user = await db.Users
                .SingleOrDefaultAsync(u => u.Id == id, ct);

            if (user is null)
                return Results.NotFound();

            user.Role = requestedRole;

            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                user.Id,
                user.Email,
                user.Role
            });
        });

        return app;
    }
}

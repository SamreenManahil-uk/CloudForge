using CloudForge.Api.Services;
using CloudForge.Api.Data;
using Microsoft.EntityFrameworkCore;
using CloudForge.Api.Endpoints;
using CloudForge.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<DeploymentStatusService>();
builder.Services.AddScoped<DeploymentClaimService>();
builder.Services.AddHostedService<DeploymentWorker>();

builder.Services.AddDbContext<CloudForgeDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CloudForgeDb")));


builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtTokenService>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtSecret) ||
    Encoding.UTF8.GetByteCount(jwtSecret) < 32 ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Valid JWT secret, issuer and audience are required.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = System.Security.Claims.ClaimTypes.Email,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "CanDeploy",
        policy => policy.RequireRole("Admin", "Developer"));

    options.AddPolicy(
        "AdminOnly",
        policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    Application = "CloudForge",
    Description = "Internal Developer Platform",
    Version = "1.0.0"
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "CloudForge API",
    Timestamp = DateTime.UtcNow
}))
.WithName("GetHealth")
.WithTags("Health");

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapApplicationEndpoints();
app.MapDeploymentEndpoints();

app.Run();

public partial class Program { }

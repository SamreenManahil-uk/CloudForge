using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CloudForge.Api.Data;

public sealed class CloudForgeDbContextFactory
    : IDesignTimeDbContextFactory<CloudForgeDbContext>
{
    public CloudForgeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CloudForgeDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cloudforge;Username=cloudforge")
            .Options;

        return new CloudForgeDbContext(options);
    }
}

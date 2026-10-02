using Microsoft.EntityFrameworkCore;

namespace CloudForge.Api.Data;

public sealed class CloudForgeDbContext : DbContext
{
    public CloudForgeDbContext(
        DbContextOptions<CloudForgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationEntity> Applications =>

        Set<ApplicationEntity>();

    public DbSet<DeploymentEntity> Deployments =>
        Set<DeploymentEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.PasswordHash).IsRequired();

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(u => u.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<ApplicationEntity>(entity =>
        {
            entity.ToTable("applications");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.RepositoryUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(x => x.Environment)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<DeploymentEntity>(entity =>
        {
            entity.ToTable("deployments");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Environment)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(d => d.Version)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne(d => d.Application)
                .WithMany()
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(d => d.ApplicationId);
        });

}
}

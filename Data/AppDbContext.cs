using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<PartCategory> PartCategories => Set<PartCategory>();
    public DbSet<VehiclePart> VehicleParts => Set<VehiclePart>();
    public DbSet<VehicleHistory> VehicleHistory => Set<VehicleHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<SmtpConfiguration> SmtpConfigurations => Set<SmtpConfiguration>();
    public DbSet<VehicleCatalogEntry> VehicleCatalogEntries => Set<VehicleCatalogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
            entity.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.CustomerNumber).IsUnique();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(x => x.InternalNumber).IsUnique();
            entity.HasIndex(x => x.Fin).IsUnique().HasFilter("\"Fin\" IS NOT NULL");
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<PartCategory>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SmtpConfiguration>(entity =>
        {
            entity.Property(x => x.FromName).HasDefaultValue("RS Fahrzeugsystem");
        });

        modelBuilder.Entity<VehicleCatalogEntry>(entity =>
        {
            entity.Property(x => x.Brand).HasMaxLength(120);
            entity.Property(x => x.Model).HasMaxLength(160);
            entity.Property(x => x.Variant).HasMaxLength(200);
            entity.Property(x => x.YearLabel).HasMaxLength(160);
            entity.Property(x => x.Engine).HasMaxLength(200);
            entity.Property(x => x.EngineCode).HasMaxLength(120);
            entity.Property(x => x.Transmission).HasMaxLength(120);
            entity.Property(x => x.TransmissionCode).HasMaxLength(120);
            entity.Property(x => x.EcuType).HasMaxLength(120);
            entity.Property(x => x.EcuManufacturer).HasMaxLength(120);
            entity.Property(x => x.DriveType).HasMaxLength(120);
            entity.Property(x => x.Platform).HasMaxLength(120);
            entity.HasIndex(x => new
            {
                x.Brand,
                x.Model,
                x.Variant,
                x.YearLabel,
                x.Engine,
                x.EngineCode
            });
        });
    }
}

using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<AppUser> AppUsers { get; set; } = null!;
    public DbSet<Asset> Assets { get; set; } = null!;
    public DbSet<AssetFile> AssetFiles { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<DepreciationMethod> DepreciationMethods { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<AssetStatus> AssetStatus { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<Building> Buildings { get; set; } = null!;
    public DbSet<AssetDepreciationHistory> AssetDepreciationHistory { get; set; } = null!;
    public DbSet<InventorySession> InventorySessions { get; set; } = null!;
    public DbSet<InventoryScan> InventoryScans { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<AppUser>(entity =>
    {
        entity.HasKey(u => u.Id);

        entity.HasIndex(u => u.Username).IsUnique();

        entity.Property(u => u.Username)
              .HasMaxLength(255)
              .IsRequired();

        entity.Property(u => u.FullName)
              .HasMaxLength(255);

        entity.Property(u => u.Role)
              .HasMaxLength(100)
              .IsRequired()
              .HasDefaultValue("User");

        entity.Property(u => u.PasswordHash)
              .HasMaxLength(255)
              .IsRequired()
              .HasDefaultValue("");  // ან წაშალე, თუ ბაზაში უკვე არის

        entity.Property(u => u.IsActive)
              .HasDefaultValue(true);

        entity.Property(u => u.CreatedAt)
              .HasDefaultValueSql("GETDATE()");

        entity.Property(u => u.CreatedBy)
              .HasMaxLength(255)
              .IsRequired();
    });

    base.OnModelCreating(builder);
}
}
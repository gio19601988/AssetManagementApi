using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // DbSet-ები — ყველა ცხრილი
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
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;
    public DbSet<AssetDepreciationHistory> AssetDepreciationHistory { get; set; } = null!;
    public DbSet<InventorySession> InventorySessions { get; set; } = null!;
    public DbSet<InventoryScan> InventoryScans { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ────────────────────────────────────────────────────────────────
        // AppUser კონფიგურაცია
        // ────────────────────────────────────────────────────────────────
        builder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(255);
            entity.Property(u => u.Role).HasMaxLength(100).IsRequired().HasDefaultValue("User");
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired().HasDefaultValue("");
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(u => u.CreatedBy).HasMaxLength(255).IsRequired();
        });


        // ────────────────────────────────────────────────────────────────
        // საწყობის მოძრაობები (StockMovement) — ურთიერთკავშირები
        // ────────────────────────────────────────────────────────────────
        builder.Entity<StockMovement>()
            .HasOne(sm => sm.Warehouse)
            .WithMany(w => w.StockMovements)
            .HasForeignKey(sm => sm.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.FromWarehouse)
            .WithMany()
            .HasForeignKey(sm => sm.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.ToWarehouse)
            .WithMany()
            .HasForeignKey(sm => sm.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.Asset)
            .WithMany()
            .HasForeignKey(sm => sm.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.ResponsiblePerson)
            .WithMany()
            .HasForeignKey(sm => sm.ResponsiblePersonId)
            .OnDelete(DeleteBehavior.SetNull);  // თანამშრომლის წაშლისას მოძრაობა რჩება

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.Supplier)
            .WithMany()
            .HasForeignKey(sm => sm.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);  // მომწოდებლის წაშლისას მოძრაობა რჩება

        // ────────────────────────────────────────────────────────────────
        // Decimal-ების სიზუსტე (precision & scale) — ფულისა და რაოდენობისთვის
        // ────────────────────────────────────────────────────────────────
        builder.Entity<Asset>(entity =>
        {
            // Decimal-ების precision
            entity.Property(a => a.PurchaseValue).HasPrecision(18, 2);
            entity.Property(a => a.SalvageValue).HasPrecision(18, 2);
            entity.Property(a => a.DisposalValue).HasPrecision(18, 2);
            entity.Property(a => a.MinStockLevel).HasPrecision(18, 2);

            // ✅ ეს ხაზი აუცილებელია - ეუბნება EF Core-ს რომელი ველია FK
            entity.HasOne(a => a.ResponsiblePerson)
                  .WithMany(e => e.ResponsibleAssets)
                  .HasForeignKey(a => a.ResponsiblePersonId)
                  .OnDelete(DeleteBehavior.SetNull);  // თუ Employee წაიშალა → NULL

            // სხვა foreign key-ები
            entity.HasOne(a => a.Category)
                  .WithMany(c => c.Assets)
                  .HasForeignKey(a => a.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Department)
                  .WithMany(d => d.Assets)
                  .HasForeignKey(a => a.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Location)
                  .WithMany(l => l.Assets)
                  .HasForeignKey(a => a.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Status)  // ← მთავარი ცვლილება: AssetStatus → Status
                  .WithMany(s => s.Assets)  // ← ეს კარგია, რადგან AssetStatus.cs-ში ICollection<Asset> Assets არსებობს
                  .HasForeignKey(a => a.AssetStatusId)  // ← დატოვე, თუ ID ასე ეწოდება
                  .OnDelete(DeleteBehavior.SetNull);  // ← optional: წაშლისას ID null გახდეს, რომ არ წაიშალოს asset


            entity.HasOne(a => a.DepreciationMethod)
                  .WithMany(dm => dm.Assets)
                  .HasForeignKey(a => a.DepreciationMethodId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Supplier)
                  .WithMany(s => s.Assets)
                  .HasForeignKey(a => a.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Parent-Child relationship
            entity.HasOne(a => a.ParentAsset)
                  .WithMany(a => a.ChildAssets)
                  .HasForeignKey(a => a.ParentAssetId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique constraints
            entity.HasIndex(a => a.Barcode).IsUnique();
            entity.HasIndex(a => a.SerialNumber).IsUnique();
        });

        builder.Entity<StockMovement>(entity =>
        {
            entity.Property(sm => sm.Quantity)
                .HasPrecision(18, 2);  // რაოდენობა — ათწილადებით
        });
        

        // ────────────────────────────────────────────────────────────────
        // იგნორირება ძველი / არასასურველი property-ების
        // ეს ხელს უშლის EF Core-ს, რომ ავტომატურად შექმნას EmployeeId სვეტი
        // ────────────────────────────────────────────────────────────────

        // ────────────────────────────────────────────────────────────────
        // სხვა მოდელების დამატებითი კონფიგურაცია (თუ საჭირო გახდა)
        // ────────────────────────────────────────────────────────────────
        // builder.Entity<Asset>()
        //     .HasOne(a => a.ResponsiblePerson)
        //     .WithMany()
        //     .HasForeignKey(a => a.ResponsiblePersonId)
        //     .OnDelete(DeleteBehavior.SetNull);
    }
}
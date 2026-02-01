using AssetManagementApi.Models;
using AssetManagementApi.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // DbSet-ები — ყველა ცხრილი
    public DbSet<AppUser> AppUsers { get; set; } = null!;
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // Orders
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }
    public DbSet<OrderType> OrderTypes { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderWorkflow> OrderWorkflows { get; set; }
    public DbSet<OrderApproval> OrderApprovals { get; set; }
    public DbSet<OrderDocument> OrderDocuments { get; set; }
    public DbSet<OrderComment> OrderComments { get; set; }

    // არსებული assets მოდულის DbSet-ები
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
        // AppUser კონფიგურაცია — არსებული ცხრილის მიბმა (მნიშვნელოვანი!)
        // ────────────────────────────────────────────────────────────────
        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");  // ← ეს ხაზი ხელს უშლის ხელახლა შექმნას
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Username).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(255);
            entity.Property(u => u.Role).HasMaxLength(100).IsRequired().HasDefaultValue("User");
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired().HasDefaultValue("");
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(u => u.CreatedBy).HasMaxLength(255).IsRequired();

            // თუ RBAC-ს იყენებ და legacy Role string არ გჭირდება — იგნორირება
            // entity.Ignore(u => u.Role);  // ← გააქტიურე თუ გინდა
        });

        // ────────────────────────────────────────────────────────────────
        // RBAC junction tables
        // ────────────────────────────────────────────────────────────────
        builder.Entity<Role>().ToTable("roles");
        builder.Entity<Permission>().ToTable("permissions");

        builder.Entity<RolePermission>()
            .ToTable("role_permissions")
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        builder.Entity<UserRole>()
            .ToTable("user_roles")
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // ────────────────────────────────────────────────────────────────
        // Orders table names (შენი SQL სქემიდან)
        // ────────────────────────────────────────────────────────────────
        builder.Entity<OrderStatus>().ToTable("order_statuses");
        builder.Entity<OrderType>().ToTable("order_types");
        builder.Entity<Order>().ToTable("orders");
        builder.Entity<OrderItem>().ToTable("order_items");
        builder.Entity<OrderWorkflow>().ToTable("order_workflow");
        builder.Entity<OrderApproval>().ToTable("order_approvals");
        builder.Entity<OrderDocument>().ToTable("order_documents");
        builder.Entity<OrderComment>().ToTable("order_comments");

        // JSON field
        builder.Entity<Order>()
            .Property(o => o.Metadata)
            .HasColumnType("nvarchar(max)");  // SQL Server-ზე JSON-ისთვის

        // ────────────────────────────────────────────────────────────────
        // Decimal precision (შენი + orders)
        // ────────────────────────────────────────────────────────────────
        builder.Entity<Order>()
            .Property(o => o.EstimatedAmount)
            .HasPrecision(15, 2);

        builder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(15, 2);

        builder.Entity<OrderItem>()
            .Property(oi => oi.TotalPrice)
            .HasPrecision(15, 2);

        builder.Entity<Asset>(entity =>
        {
            entity.Property(a => a.PurchaseValue).HasPrecision(18, 2);
            entity.Property(a => a.SalvageValue).HasPrecision(18, 2);
            entity.Property(a => a.DisposalValue).HasPrecision(18, 2);
            entity.Property(a => a.MinStockLevel).HasPrecision(18, 2);
        });

        builder.Entity<StockMovement>(entity =>
        {
            entity.Property(sm => sm.Quantity).HasPrecision(18, 2);
        });

        // ────────────────────────────────────────────────────────────────
        // შენი არსებული კონფიგურაციები (StockMovement, Asset FK-ები და ა.შ.)
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
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.Supplier)
            .WithMany()
            .HasForeignKey(sm => sm.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        // Asset კონფიგურაცია (შენი არსებული)
        builder.Entity<Asset>(entity =>
        {
            entity.HasOne(a => a.ResponsiblePerson)
                  .WithMany(e => e.ResponsibleAssets)
                  .HasForeignKey(a => a.ResponsiblePersonId)
                  .OnDelete(DeleteBehavior.SetNull);

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

            entity.HasOne(a => a.Status)
                  .WithMany(s => s.Assets)
                  .HasForeignKey(a => a.AssetStatusId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.DepreciationMethod)
                  .WithMany(dm => dm.Assets)
                  .HasForeignKey(a => a.DepreciationMethodId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Supplier)
                  .WithMany(s => s.Assets)
                  .HasForeignKey(a => a.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.ParentAsset)
                  .WithMany(a => a.ChildAssets)
                  .HasForeignKey(a => a.ParentAssetId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.Barcode).IsUnique();
            entity.HasIndex(a => a.SerialNumber).IsUnique();
        });

        // დამატებით table names სხვა entities-ზე (თუ DB-ში სხვა სახელებია — შეცვალე)
        builder.Entity<Asset>().ToTable("assets");
        builder.Entity<Department>().ToTable("departments");
        builder.Entity<Employee>().ToTable("employees");
        // დაამატე სხვა თუ საჭიროა, მაგ. Warehouses → "warehouses"
    }
}
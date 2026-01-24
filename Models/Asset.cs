using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models;

public class Asset
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string AssetName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? SearchDescription { get; set; }

    [MaxLength(255)]
    public string? Manufacturer { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(150)]
    public string? SerialNumber { get; set; }

    [MaxLength(100)]
    public string? InventoryNumber { get; set; }

    public string? ScannedBarcodeResponse { get; set; }

    // Foreign Keys
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public int? LocationId { get; set; }
    public int? AssetStatusId { get; set; }
    
    // ✅ КРИТИЧНО - ეს არის სწორი ველი!
    public int? ResponsiblePersonId { get; set; }

    // Purchase Info
    public DateTime? PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PurchaseValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalvageValue { get; set; }

    public int? UsefulLifeMonths { get; set; }

    // Depreciation
    public int? DepreciationMethodId { get; set; }
    public DateTime? DepreciationStartDate { get; set; }

    [MaxLength(100)]
    public string? DepreciationBook { get; set; }

    [MaxLength(50)]
    public string? AssetAccount { get; set; }

    [MaxLength(50)]
    public string? DepreciationAccount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DisposalValue { get; set; }

    public int? SupplierId { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    public int? ParentAssetId { get; set; }

    public string? CustomFieldsJson { get; set; }
    public string? Notes { get; set; }

    // Medical/Pharmacy fields
    [MaxLength(100)]
    public string? DosageForm { get; set; }

    [MaxLength(100)]
    public string? Concentration { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinStockLevel { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    // ===================================
    // Navigation Properties
    // ===================================
    
    // ✅ КРИТИЧНО - ForeignKey ატრიბუტი რომ EF Core არ დაბნდეს!
    [ForeignKey(nameof(ResponsiblePersonId))]
    public Employee? ResponsiblePerson { get; set; }

    public Category? Category { get; set; }
    public Department? Department { get; set; }
    public Location? Location { get; set; }
    public AssetStatus? Status { get; set; }
    public DepreciationMethod? DepreciationMethod { get; set; }
    public Supplier? Supplier { get; set; }

    [ForeignKey(nameof(ParentAssetId))]
    public Asset? ParentAsset { get; set; }

    // Collections
    public ICollection<Asset> ChildAssets { get; set; } = new List<Asset>();
    public ICollection<AssetFile> AssetFiles { get; set; } = new List<AssetFile>();
    public ICollection<AssetDepreciationHistory> DepreciationHistory { get; set; } = new List<AssetDepreciationHistory>();
}
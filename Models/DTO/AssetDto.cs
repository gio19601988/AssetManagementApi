namespace AssetManagementApi.Models.DTO;

public class AssetDto
{
    public int Id { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SearchDescription { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? Barcode { get; set; }
    public string? SerialNumber { get; set; }
    public string? InventoryNumber { get; set; }
    public string? ScannedBarcodeResponse { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? AssetStatusId { get; set; }
    public string? StatusName { get; set; }
    public int? ResponsiblePersonId { get; set; }
    public string? ResponsiblePersonName { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public decimal? SalvageValue { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public int? DepreciationMethodId { get; set; }
    public string? DepreciationMethodName { get; set; }
    public DateTime? DepreciationStartDate { get; set; }
    public string? DepreciationBook { get; set; }
    public string? AssetAccount { get; set; }
    public string? DepreciationAccount { get; set; }
    public decimal? DisposalValue { get; set; }
    public int? SupplierId { get; set; }
    public string? Currency { get; set; }
    public int? ParentAssetId { get; set; }
    public string? CustomFieldsJson { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class AssetCreateDto
{
    public string AssetName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SearchDescription { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? Barcode { get; set; }
    public string? SerialNumber { get; set; }
    public string? InventoryNumber { get; set; }
    public string? ScannedBarcodeResponse { get; set; }
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public int? LocationId { get; set; }
    public int? AssetStatusId { get; set; }
    public int? ResponsiblePersonId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public decimal? SalvageValue { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public int? DepreciationMethodId { get; set; }
    public DateTime? DepreciationStartDate { get; set; }
    public string? DepreciationBook { get; set; }
    public string? AssetAccount { get; set; }
    public string? DepreciationAccount { get; set; }
    public decimal? DisposalValue { get; set; }
    public int? SupplierId { get; set; }
    public string? Currency { get; set; }
    public int? ParentAssetId { get; set; }
    public string? CustomFieldsJson { get; set; }
    public string? Notes { get; set; }
}

public class AssetUpdateDto : AssetCreateDto
{
    public int Id { get; set; }
}
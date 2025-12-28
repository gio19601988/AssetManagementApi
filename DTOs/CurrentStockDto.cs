namespace AssetManagementApi.DTOs
{
    public class CurrentStockDto
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? SerialNumber { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? WarehouseLevel { get; set; }
        public decimal CurrentQuantity { get; set; }
        public string? CategoryName { get; set; }
        public string? DepartmentName { get; set; }
    }
}
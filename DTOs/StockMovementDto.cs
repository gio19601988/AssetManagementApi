namespace AssetManagementApi.DTOs;

public class StockMovementDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;  // In, Out, Transfer
    public int? FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public int? ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? ResponsiblePersonId { get; set; }
    public string? ResponsiblePersonName { get; set; }
    public string? ReferenceDocument { get; set; }  // ზედდებული/ფაქტურის ნომერი
    public DateTime MovementDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
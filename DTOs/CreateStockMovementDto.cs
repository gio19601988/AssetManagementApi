namespace AssetManagementApi.DTOs;
public class CreateStockMovementDto
    {
        public int AssetId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public string MovementType { get; set; } = string.Empty;  // In, Out, Transfer
        public int? FromWarehouseId { get; set; }
        public int? ToWarehouseId { get; set; }
        public int? SupplierId { get; set; }
        public int? ResponsiblePersonId { get; set; }
        public string? ReferenceDocument { get; set; }
        public DateTime? MovementDate { get; set; }
        public string? Notes { get; set; }
    }
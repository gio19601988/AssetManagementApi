using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models
{
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AssetId { get; set; }
        public Asset Asset { get; set; } = null!;  // შენი არსებული Assets

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string MovementType { get; set; } = string.Empty;  // "In", "Out", "Transfer"

        public int? FromWarehouseId { get; set; }
        public Warehouse? FromWarehouse { get; set; }

        public int? ToWarehouseId { get; set; }
        public Warehouse? ToWarehouse { get; set; }

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }  // შენი არსებული Suppliers

        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }  // ვინ განახორციელა

        [MaxLength(100)]
        public string? ReferenceDocument { get; set; }  // ზედდებული/ფაქტურის ნომერი

        public DateTime MovementDate { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
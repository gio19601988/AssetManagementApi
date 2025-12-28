// Models/Warehouse.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Code { get; set; }

        [MaxLength(50)]
        public string? Level { get; set; }  // "ცენტრალური", "ფილიალი"

        public int? LocationId { get; set; }
        public Building? Location { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? ResponsiblePersonId { get; set; }
        public Employee? ResponsiblePerson { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
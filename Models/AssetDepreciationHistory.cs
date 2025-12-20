// Models/AssetDepreciationHistory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  // ← ეს დაამატე!

namespace AssetManagementApi.Models
{
    public class AssetDepreciationHistory
    {
        [Key]
        public int DepreciationID { get; set; }

        public int AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        public DateTime DepreciationDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]  // ← ეს არის სწორი, Precision-ის ნაცვლად
        public decimal Amount { get; set; }

        public string? DepreciationBook { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public Guid? ManualRunSessionID { get; set; }

        public string? Notes { get; set; }
    }
}
// Models/Orders/OrderDocument.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Orders
{
    public class OrderDocument
    {
        internal DateTime UpdatedAt;

        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public long? FileSize { get; set; }
        [MaxLength(100)]
        public string? FileType { get; set; }

        public int? UploadedBy { get; set; }
        // public ApplicationUser? UploadedByUser { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
    }
}
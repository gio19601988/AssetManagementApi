// Models/Orders/OrderItem.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Orders
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }  // navigation

        public int? AssetId { get; set; }  // თუ არსებული asset-ია
        // public Asset? Asset { get; set; }  // თუ assets მოდული გაქვს

        [MaxLength(255)]
        public string? ItemName { get; set; }

        public string? ItemDescription { get; set; }

        public int? CategoryId { get; set; }
        // public AssetCategory? Category { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
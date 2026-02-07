// Models/Orders/OrderWorkflow.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Orders
{
    public class OrderWorkflow
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int? FromStatusId { get; set; }
        public OrderStatus? FromStatus { get; set; }

        public int? ToStatusId { get; set; }
        public OrderStatus? ToStatus { get; set; } = null!;  // ← ეს დაამატე ან გაასწორე

        public int? ChangedBy { get; set; }
        // public ApplicationUser? ChangedByUser { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Comments { get; set; }
        public string? Metadata { get; set; }  // jsonb
    }
}
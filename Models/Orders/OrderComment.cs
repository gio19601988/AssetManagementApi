// Models/Orders/OrderComment.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Orders
{
    public class OrderComment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int UserId { get; set; }
        // public ApplicationUser? User { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        public bool IsInternal { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
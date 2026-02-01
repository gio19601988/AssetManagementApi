// Models/Orders/OrderApproval.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Orders
{
    public class OrderApproval
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ApprovalLevel { get; set; }

        public int? ApproverId { get; set; }
        // public ApplicationUser? Approver { get; set; }

        public int? ApproverRoleId { get; set; }
        public Role? ApproverRole { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "pending";  // pending, approved, rejected

        public string? Comments { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}
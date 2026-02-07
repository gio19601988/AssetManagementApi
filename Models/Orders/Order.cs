// Models/Orders/Order.cs
using System.ComponentModel.DataAnnotations;
using AssetManagementApi.Models;  // <--- ახალი ხაზი

namespace AssetManagementApi.Models.Orders;

    public class Order
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty; // ავტომატურად გენერირდება

        public int? OrderTypeId { get; set; }
        public OrderType? OrderType { get; set; }

        public int StatusId { get; set; }
        public OrderStatus? Status { get; set; }

        public int RequesterId { get; set; }
        // public ApplicationUser Requester { get; set; }

        public int? DepartmentId { get; set; }
        // public Department Department { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(20)]
        public string Priority { get; set; } = "medium"; // low, medium, high

        public decimal? EstimatedAmount { get; set; }
        public string Currency { get; set; } = "GEL";

        public DateTime RequestedDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? RequiredByDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? Metadata { get; set; } // JSONB-სთვის string ან Dictionary

        // Navigation
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderWorkflow> WorkflowHistories { get; set; } = new List<OrderWorkflow>();
        public ICollection<OrderApproval> Approvals { get; set; } = new List<OrderApproval>();
        public ICollection<OrderDocument> Documents { get; set; } = new List<OrderDocument>();
        public ICollection<OrderComment> Comments { get; set; } = new List<OrderComment>();
    }
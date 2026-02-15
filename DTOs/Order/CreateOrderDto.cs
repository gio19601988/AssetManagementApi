// Dtos/Orders/CreateOrderDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class CreateOrderDto
    {
        [Required]
        public int OrderTypeId { get; set; } // ტიპი (order_types-დან)

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(20)]
        public string Priority { get; set; } = "medium";

        public decimal? EstimatedAmount { get; set; }
        public string Currency { get; set; } = "GEL";

        [Required]
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow.Date;

        public DateTime? RequiredByDate { get; set; }

        public int? DepartmentId { get; set; }
        public bool NotifyApprovers { get; set; } = false;  // ← ახალი

        // Items (ახალი შეკვეთისთვის list)
        public List<CreateOrderItemDto> Items { get; set; } = new List<CreateOrderItemDto>();
    }
}
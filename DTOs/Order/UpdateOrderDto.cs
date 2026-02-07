// Dtos/Orders/UpdateOrderDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class UpdateOrderDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public decimal? EstimatedAmount { get; set; }
        public string? Currency { get; set; }
        public DateTime? RequestedDate { get; set; }
        public DateTime? RequiredByDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? OrderTypeId { get; set; }
        // Items-ის სრული რედაქტირებისთვის მოგვიანებით დავამატებთ
    }
}
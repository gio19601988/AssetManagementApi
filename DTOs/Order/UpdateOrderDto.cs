// Dtos/Orders/UpdateOrderDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class UpdateOrderDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Priority { get; set; }

        public decimal? EstimatedAmount { get; set; }

        public DateTime? RequiredByDate { get; set; }
    }
}
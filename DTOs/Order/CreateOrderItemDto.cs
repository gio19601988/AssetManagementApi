// Dtos/Orders/CreateOrderItemDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class CreateOrderItemDto
    {
        public int? AssetId { get; set; }

        [Required, MaxLength(255)]
        public string ItemName { get; set; } = string.Empty;

        public string? ItemDescription { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "რაოდენობა უნდა იყოს მინიმუმ 1")]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "ფასი არ შეიძლება იყოს უარყოფითი")]
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }

        public string? Notes { get; set; }
    }
}
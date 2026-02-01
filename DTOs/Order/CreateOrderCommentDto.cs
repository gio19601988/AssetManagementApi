// Dtos/Orders/CreateOrderCommentDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class CreateOrderCommentDto
    {
        [Required]
        public string Comment { get; set; } = string.Empty;

        public bool IsInternal { get; set; } = false;
    }
}
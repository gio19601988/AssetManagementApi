// Dtos/Orders/UpdateOrderCommentDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class UpdateOrderCommentDto
    {
        [Required]  // აუცილებელია კომენტარის განახლება
        public string Comment { get; set; } = string.Empty;  // განახლებული კომენტარის ტექსტი

        public bool? IsInternal { get; set; }  // შესაძლო განახლება: შიდა თუ საჯარო
    }
}
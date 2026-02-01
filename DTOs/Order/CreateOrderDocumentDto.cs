// Dtos/Orders/CreateOrderDocumentDto.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Dtos.Orders
{
    public class CreateOrderDocumentDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;  // ფაილი ატვირთვისთვის (Controller-ში გამოიყენე [FromForm])

        public string? Description { get; set; }
    }
}
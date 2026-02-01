// Dtos/Orders/OrderItemDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? AssetId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemDescription { get; set; }
        public int? CategoryId { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

 

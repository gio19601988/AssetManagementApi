// Dtos/Orders/OrderStatusDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderStatusDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameKa { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int OrderSeq { get; set; }
        public bool IsActive { get; set; }
    }
}
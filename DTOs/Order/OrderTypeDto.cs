// Dtos/Orders/OrderTypeDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameKa { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public int ApprovalLevels { get; set; }
    }
}
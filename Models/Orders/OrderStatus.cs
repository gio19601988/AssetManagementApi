// Models/Orders/OrderStatus.cs
using System.ComponentModel.DataAnnotations;
namespace AssetManagementApi.Models
{
    public class OrderStatus
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameKa { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int OrderSeq { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
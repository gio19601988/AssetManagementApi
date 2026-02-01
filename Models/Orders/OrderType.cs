// Models/Orders/OrderType.cs
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models.Orders
{
    public class OrderType
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string NameKa { get; set; } = string.Empty;

        public bool RequiresApproval { get; set; } = true;
        public int ApprovalLevels { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
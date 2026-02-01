// Dtos/Orders/OrderDetailDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        
        // Type Info
        public int? OrderTypeId { get; set; }
        public string? OrderTypeName { get; set; }
        public string? OrderTypeNameKa { get; set; }
        
        // Status Info
        public int StatusId { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string StatusNameKa { get; set; } = string.Empty;
        
        // Basic Info
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        
        // Financial
        public decimal? EstimatedAmount { get; set; }
        public string Currency { get; set; } = "GEL";
        
        // Dates
        public DateTime RequestedDate { get; set; }
        public DateTime? RequiredByDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Items
        public List<OrderItemDto> Items { get; set; } = new();
    }

    
}
// Dtos/Orders/OrderDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "medium";
        public decimal? EstimatedAmount { get; set; }
        public string Currency { get; set; } = "GEL";
        public DateTime RequestedDate { get; set; }
        public DateTime? RequiredByDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Status და Type სახელები (ქართულად თუ გინდა)
        public string StatusNameKa { get; set; } = string.Empty;
        public string TypeNameKa { get; set; } = string.Empty;

        // Requester info (მარტივი)
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;

        // Items, Comments, Documents (list-ისთვის short, დეტალებისთვის full)
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public List<OrderDocumentDto> Documents { get; set; } = new List<OrderDocumentDto>();
        public List<OrderCommentDto> Comments { get; set; } = new List<OrderCommentDto>();
        public List<WorkflowHistoryDto>? WorkflowHistory { get; set; }
        // დამატე თუ გინდა Comments, Documents DTO-ები მსგავსად
    }
}
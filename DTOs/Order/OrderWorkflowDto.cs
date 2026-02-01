// Dtos/Orders/OrderWorkflowDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderWorkflowDto
    {
        public int Id { get; set; }  // ისტორიის ID

        public string? FromStatusNameKa { get; set; }  // წინა სტატუსი (ქართულად)

        public string ToStatusNameKa { get; set; } = string.Empty;  // ახალი სტატუსი (ქართულად)

        public int? ChangedBy { get; set; }  // ვინ შეცვალა

        public string? ChangedByName { get; set; }  // შემცვლელის სახელი

        public DateTime ChangedAt { get; set; }  // ცვლილების თარიღი

        public string? Comments { get; set; }  // კომენტარი ცვლილებაზე

        public string? Metadata { get; set; }  // დამატებითი მონაცემები (JSON)
    }
}
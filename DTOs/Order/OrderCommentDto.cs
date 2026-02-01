// Dtos/Orders/OrderCommentDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderCommentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;  // commenter-ის სახელი
        public string Comment { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
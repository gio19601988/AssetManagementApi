// Dtos/Orders/OrderDocumentDto.cs
namespace AssetManagementApi.Dtos.Orders
{
    public class OrderDocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;  // URL ან path ფაილის წვდომისთვის
        public long? FileSize { get; set; }
        public string? FileType { get; set; }
        public int? UploadedBy { get; set; }
        public string? UploadedByName { get; set; }  // optional, user-ის სახელი
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
    }
}
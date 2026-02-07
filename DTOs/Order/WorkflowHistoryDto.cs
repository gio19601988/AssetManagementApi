// Dtos/Orders/WorkflowHistoryDto.cs
namespace AssetManagementApi.Dtos.Orders
{
   public class WorkflowHistoryDto
   {
      public string? FromStatusNameKa { get; set; }
      public string ToStatusNameKa { get; set; } = string.Empty;
      public string? ChangedByName { get; set; }
      public DateTime ChangedAt { get; set; }
      public string? Comments { get; set; }
   }
}
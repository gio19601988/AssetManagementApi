namespace AssetManagementApi.DTOs
{
    public class DepreciationHistoryDto
    {
        public int DepreciationID { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public DateTime DepreciationDate { get; set; }
        public decimal Amount { get; set; }
        public string? DepreciationBook { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? Notes { get; set; }
    }
}
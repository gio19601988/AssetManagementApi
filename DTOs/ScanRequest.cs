namespace AssetManagementApi.DTOs;
public class ScanRequest
    {
        public int SessionId { get; set; }
        public string Barcode { get; set; } = string.Empty;
    }
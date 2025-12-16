using AssetManagementApi.Models;  // <--- ახალი ხაზი

namespace AssetManagementApi.Models;

public class AssetFile
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!;
    
    public string? FileCategory { get; set; }
    public DateTime UploadDate { get; set; }

    public AssetManagementApi.Models.Asset Asset { get; set; } = null!;
}
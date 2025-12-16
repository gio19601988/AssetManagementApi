namespace AssetManagementApi.DTOs;

public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public int AssetId { get; set; }
    public string? FileCategory { get; set; }
}
namespace AssetManagementApi.DTOs;

public record AssetFileDto(
    int Id,
    int AssetId,
    string FileName,
    string FileUrl,
    string FileType,
    string? FileCategory,
    DateTime UploadDate
);
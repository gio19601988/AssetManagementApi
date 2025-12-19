namespace AssetManagementApi.DTOs;

public record DepreciationMethodCreateDto(
    string Name,
    string Code,
    string? Description = null,
    bool IsActive = true
);
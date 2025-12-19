namespace AssetManagementApi.DTOs;

public record CategoryCreateDto(
    string Name,
    string Code,
    int? ParentCategoryId = null,
    string? Description = null,
    bool IsActive = true
);
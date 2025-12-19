namespace AssetManagementApi.DTOs;

public record CategoryUpdateDto(
    string? Name = null,
    string? Code = null,
    int? ParentCategoryId = null,
    string? Description = null,
    bool? IsActive = null
);
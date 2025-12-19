namespace AssetManagementApi.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string Code,
    int? ParentCategoryId,
    string? ParentCategoryName,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);
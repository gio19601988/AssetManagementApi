namespace AssetManagementApi.DTOs;

public record DepartmentDto(
    int Id,
    string Name,
    string? Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);
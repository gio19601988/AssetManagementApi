namespace AssetManagementApi.DTOs;

public record DepartmentCreateDto(
    string Name,
    string? Code = null,
    string? Description = null,
    bool IsActive = true
);
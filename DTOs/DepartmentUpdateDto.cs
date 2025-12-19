namespace AssetManagementApi.DTOs;

public record DepartmentUpdateDto(
    string? Name = null,
    string? Code = null,
    string? Description = null,
    bool? IsActive = null
);
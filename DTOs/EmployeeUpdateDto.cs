namespace AssetManagementApi.DTOs;

public record EmployeeUpdateDto(
    string? FullName = null,
    string? Position = null,
    int? DepartmentId = null,
    string? Phone = null,
    string? Email = null
);
namespace AssetManagementApi.DTOs;

public record EmployeeCreateDto(
    string FullName,
    string? Position = null,
    int? DepartmentId = null,
    string? Phone = null,
    string? Email = null
);
namespace AssetManagementApi.DTOs;

public record EmployeeDto(
    int Id,
    string Name,              // ახალი — FullName-ის ასლი
    string FullName,
    string? Position,
    int? DepartmentId,
    string? DepartmentName,
    string? Phone,
    string? Email,
    DateTime CreatedAt
);
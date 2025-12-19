namespace AssetManagementApi.DTOs;

public record SupplierDto(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    string? Code
);
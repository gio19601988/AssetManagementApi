namespace AssetManagementApi.DTOs;

public record SupplierCreateDto(
    string Name,
    string? ContactPerson = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Notes = null,
    string? Code = null,
    bool IsActive = true
);
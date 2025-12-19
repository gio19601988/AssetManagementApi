namespace AssetManagementApi.DTOs;

public record SupplierUpdateDto(
    string? Name = null,
    string? ContactPerson = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Notes = null,
    string? Code = null,
    bool? IsActive = null
);
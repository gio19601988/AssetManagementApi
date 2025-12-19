namespace AssetManagementApi.DTOs;

public record DepreciationMethodUpdateDto(
    string? Name = null,
    string? Code = null,
    string? Description = null,
    bool? IsActive = null
);
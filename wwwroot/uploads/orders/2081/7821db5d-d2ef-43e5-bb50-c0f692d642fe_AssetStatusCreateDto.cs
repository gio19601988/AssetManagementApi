namespace AssetManagementApi.DTOs;

public record AssetStatusCreateDto(
    string StatusName,
    string? Code = null,
    string? Description = null,
    bool IsActive = true
);
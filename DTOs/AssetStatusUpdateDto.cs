namespace AssetManagementApi.DTOs;

public record AssetStatusUpdateDto(
    string? StatusName = null,
    string? Code = null,
    string? Description = null,
    bool? IsActive = null
);
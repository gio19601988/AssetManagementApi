namespace AssetManagementApi.DTOs;

public record AssetStatusDto(
    int Id,
    string Name,  // StatusName → Name
    string? Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);
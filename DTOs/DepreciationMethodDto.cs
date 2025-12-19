namespace AssetManagementApi.DTOs;

public record DepreciationMethodDto(
    int Id,
    string Name,  // <--- MethodName → Name
    string? Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);
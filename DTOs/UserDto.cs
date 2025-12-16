namespace AssetManagementApi.DTOs;

public record UserDto(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    DateTime? CreatedAt = null
);
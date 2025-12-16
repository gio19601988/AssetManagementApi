namespace AssetManagementApi.DTOs;

public record UpdateUserRequest(
    string? Role = null,
    bool IsActive = true
);
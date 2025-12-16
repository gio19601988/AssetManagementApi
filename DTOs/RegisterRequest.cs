namespace AssetManagementApi.DTOs;

public record RegisterRequest(
    string Username,
    string Password,
    string? FullName = null,
    string Role = "User",  // default User
    bool IsActive = true
);
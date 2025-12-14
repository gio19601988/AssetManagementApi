using AssetManagementApi.DTOs;

namespace AssetManagementApi.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> ChangePasswordAsync(int userId, string newPassword);
}
namespace AssetManagementApi.DTOs;
public record ChangePasswordRequest(string OldPassword, string NewPassword);
namespace AssetManagementApi.DTOs;

public record RegisterResponse(
    int Id,
    string Username,
    string FullName,
    string Role,
    string Message = "მომხმარებელი წარმატებით შეიქმნა"
);
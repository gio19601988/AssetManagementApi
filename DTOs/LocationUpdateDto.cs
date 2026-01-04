namespace AssetManagementApi.DTOs;
// DTOs/LocationUpdateDto.cs
public record LocationUpdateDto(
        int BuildingId,
    string RoomNumber,
    string? Description = null,
    bool IsActive = true
);
namespace AssetManagementApi.DTOs;
// DTOs/LocationCreateDto.cs
public record LocationCreateDto(
    int BuildingId,
    string RoomNumber,
    string? Description = null,
    bool IsActive = true
);
namespace AssetManagementApi.DTOs;

public record LocationCreateDto(
    int BuildingId,
    string RoomNumber,
    bool IsActive = true
);
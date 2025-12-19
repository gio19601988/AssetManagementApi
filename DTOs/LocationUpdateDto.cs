namespace AssetManagementApi.DTOs;

public record LocationUpdateDto(
    int? BuildingId = null,
    string? RoomNumber = null,
    bool? IsActive = null
);
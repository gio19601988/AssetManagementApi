namespace AssetManagementApi.DTOs;

public record LocationDto(
    int Id,
    string Name,              // ახალი — Building + Room
    int BuildingId,
    string BuildingName,
    string RoomNumber,
    bool IsActive,
    DateTime CreatedAt
);
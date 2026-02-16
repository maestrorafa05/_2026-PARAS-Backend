namespace PARAS.Api.DTOs;

public record RoomResponse(
    Guid Id,
    string Code,
    string Name,
    string? Location,
    int Capacity,
    string? Facilities,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateRoomRequest(
    string Code,
    string Name,
    string? Location,
    int Capacity,
    string? Facilities
);

public record UpdateRoomRequest(
    string Code,
    string Name,
    string? Location,
    int Capacity,
    string? Facilities,
    bool IsActive
);
using PARAS.Api.Domain.Enums;

namespace PARAS.Api.DTOs;

public record LoanResponse(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    string RoomName,
    string NamaPeminjam,
    string NRP,
    DateTime StartTime,
    DateTime EndTime,
    LoanStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateLoanRequest(
    Guid RoomId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
);

public record UpdateLoanRequest(
    Guid RoomId,
    string NamaPeminjam,
    string NRP,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
);
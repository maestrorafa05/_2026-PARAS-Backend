namespace PARAS.Api.DTOs.Users;

public record UserResponse(
    Guid Id,
    string Nrp,
    string? FullName,
    string[] Roles
);

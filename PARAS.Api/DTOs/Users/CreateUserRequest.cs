namespace PARAS.Api.DTOs.Users;

public record CreateUserRequest(
    string Nrp,
    string? FullName,
    string Password,
    string? Role
);

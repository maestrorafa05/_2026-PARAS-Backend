namespace PARAS.Api.DTOs.Auth;

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInMinutes,
    string UserId,
    string Nrp,
    string? FullName,
    string[] Roles
);

namespace PARAS.Api.DTOs.Auth;

public record LoginRequest(
    string Nrp,
    string Password
);

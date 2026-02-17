using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.DTOs.Auth;
using PARAS.Api.Auth;
using PARAS.Api.Services.Auth;

namespace PARAS.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        // endpoint untuk login (NRP + password) -> JWT
        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            JwtTokenService tokenService) =>
        {
            // validasi input
            if (string.IsNullOrWhiteSpace(req.Nrp))
                return Results.BadRequest("NRP wajib diisi.");
            if (string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest("Password wajib diisi.");

            var nrp = req.Nrp.Trim();

            // cari user berdasarkan NRP
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Nrp == nrp);
            if (user is null)
                return Results.Unauthorized();

            // verifikasi password
            var ok = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
            if (!ok.Succeeded)
                return Results.Unauthorized();

            var (token, expiresMin, roles) = await tokenService.CreateTokenAsync(user);

            return Results.Ok(new LoginResponse(
                AccessToken: token,
                TokenType: "Bearer",
                ExpiresInMinutes: expiresMin,
                UserId: user.Id.ToString(),
                Nrp: user.Nrp,
                FullName: user.FullName,
                Roles: roles
            ));
        });

        // endpoint untuk cek user yang sedang login (JWT harus valid)
        group.MapGet("/me", async (HttpContext ctx, UserManager<AppUser> userManager) =>
        {
            var guid = ctx.User.GetUserId();
            if (guid == Guid.Empty)
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(guid.ToString());
            if (user is null)
                return Results.Unauthorized();

            var roles = (await userManager.GetRolesAsync(user)).ToArray();

            return Results.Ok(new
            {
                userId = user.Id,
                nrp = user.Nrp,
                fullName = user.FullName,
                roles
            });
        }).RequireAuthorization();

        return app;
    }
}

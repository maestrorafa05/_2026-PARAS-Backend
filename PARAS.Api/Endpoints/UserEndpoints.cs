using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Auth;
using PARAS.Api.DTOs.Users;

namespace PARAS.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // group endpoint untuk user management, hanya bisa diakses oleh admin
        var group = app.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization("AdminOnly"); 

        // endpoint untuk mendapatkan daftar semua user
        group.MapGet("/", async (UserManager<AppUser> userManager) =>
        {
            var users = await userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.Nrp)
                .ToListAsync();

            var result = new List<UserResponse>();

            foreach (var u in users)
            {
                var roles = (await userManager.GetRolesAsync(u)).ToArray();
                result.Add(new UserResponse(u.Id, u.Nrp, u.FullName, roles));
            }

            return Results.Ok(result);
        });

        // endpoint untuk mendapatkan detail user berdasarkan id
        group.MapGet("/{id:guid}", async (Guid id, UserManager<AppUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user is null) return Results.NotFound();

            var roles = (await userManager.GetRolesAsync(user)).ToArray();
            return Results.Ok(new UserResponse(user.Id, user.Nrp, user.FullName, roles));
        });

        // endpoint untuk membuat user baru (default role: User)
        group.MapPost("/", async (
            CreateUserRequest req,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager) =>
        {
            // validasi input
            if (string.IsNullOrWhiteSpace(req.Nrp))
                return Results.BadRequest("NRP wajib diisi.");
            if (string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest("Password wajib diisi.");

            var nrp = req.Nrp.Trim();
            var role = string.IsNullOrWhiteSpace(req.Role) ? "User" : req.Role.Trim();

            // validasi role harus ada
            if (!await roleManager.RoleExistsAsync(role))
                return Results.BadRequest($"Role '{role}' tidak ditemukan. Gunakan 'Admin' atau 'User'.");

            // cek NRP unique
            var exists = await userManager.Users.AnyAsync(u => u.Nrp == nrp);
            if (exists)
                return Results.Conflict($"NRP '{nrp}' sudah terdaftar.");

            // buat user
            var user = new AppUser
            {
                Nrp = nrp,
                UserName = nrp, // login pakai NRP
                FullName = req.FullName?.Trim()
            };

            var create = await userManager.CreateAsync(user, req.Password);
            if (!create.Succeeded)
            {
                var errors = create.Errors.Select(e => new { e.Code, e.Description });
                return Results.BadRequest(new { errors });
            }

            // set role user
            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                var errors = addRole.Errors.Select(e => new { e.Code, e.Description });
                return Results.BadRequest(new { errors });
            }

            var roles = (await userManager.GetRolesAsync(user)).ToArray();
            return Results.Created($"/users/{user.Id}", new UserResponse(user.Id, user.Nrp, user.FullName, roles));
        });

        // endpoint untuk mengubah role user (single-role: Admin/User)
        group.MapPatch("/{id:guid}/role", async (
            Guid id,
            UpdateUserRoleRequest req,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager) =>
        {
            if (string.IsNullOrWhiteSpace(req.Role))
                return Results.BadRequest("Role wajib diisi.");

            var role = req.Role.Trim();
            if (!await roleManager.RoleExistsAsync(role))
                return Results.BadRequest($"Role '{role}' tidak ditemukan. Gunakan 'Admin' atau 'User'.");

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user is null) return Results.NotFound();

            // kita anggap 1 user hanya punya 1 role utama
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
                await userManager.RemoveFromRolesAsync(user, currentRoles);

            var add = await userManager.AddToRoleAsync(user, role);
            if (!add.Succeeded)
            {
                var errors = add.Errors.Select(e => new { e.Code, e.Description });
                return Results.BadRequest(new { errors });
            }

            var roles = (await userManager.GetRolesAsync(user)).ToArray();
            return Results.Ok(new UserResponse(user.Id, user.Nrp, user.FullName, roles));
        });

        return app;
    }
}

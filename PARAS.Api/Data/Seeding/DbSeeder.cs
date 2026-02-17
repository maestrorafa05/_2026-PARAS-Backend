using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Auth;
using PARAS.Api.Options;

namespace PARAS.Api.Data.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var adminOpt = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        // 1) Seed roles
        var roles = new[] { "Admin", "User" };

        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
            {
                await roleManager.CreateAsync(new AppRole { Name = r });
            }
        }

        static string BuildIdentityErrors(IEnumerable<IdentityError> errors)
            => string.Join("; ", errors.Select(e => $"{e.Code}:{e.Description}"));

        async Task EnsurePasswordAndUnlockAsync(AppUser user, string password, string label)
        {
            var passwordOk = await userManager.CheckPasswordAsync(user, password);
            if (!passwordOk)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await userManager.ResetPasswordAsync(user, token, password);
                if (!reset.Succeeded)
                    throw new InvalidOperationException($"Reset password {label} failed: {BuildIdentityErrors(reset.Errors)}");
            }

            // pastikan user tidak terkunci karena percobaan login sebelumnya
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.SetLockoutEndDateAsync(user, null);
        }

        // 2) Seed admin default
        if (!string.IsNullOrWhiteSpace(adminOpt.Nrp) && !string.IsNullOrWhiteSpace(adminOpt.Password))
        {
            var nrp = adminOpt.Nrp.Trim();
            var existing = await userManager.Users.FirstOrDefaultAsync(u => u.Nrp == nrp);

            if (existing is null)
            {
                var fullName = string.IsNullOrWhiteSpace(adminOpt.FullName) ? nrp : adminOpt.FullName.Trim();

                var admin = new AppUser
                {
                    Nrp = nrp,
                    UserName = nrp,
                    FullName = fullName
                };

                var create = await userManager.CreateAsync(admin, adminOpt.Password);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException($"Seed admin failed: {BuildIdentityErrors(create.Errors)}");
                }

                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                var fullName = string.IsNullOrWhiteSpace(adminOpt.FullName) ? nrp : adminOpt.FullName.Trim();
                if (string.IsNullOrWhiteSpace(existing.FullName))
                {
                    existing.FullName = fullName;
                    await userManager.UpdateAsync(existing);
                }

                await EnsurePasswordAndUnlockAsync(existing, adminOpt.Password, $"admin {nrp}");

                var isAdmin = await userManager.IsInRoleAsync(existing, "Admin");
                if (!isAdmin)
                    await userManager.AddToRoleAsync(existing, "Admin");
            }
        }

        // 3) Seed beberapa user default untuk pengujian antar-user
        var defaultUsers = new[]
        {
            new { Nrp = "3124600015", Password = "12345678", FullName = "User 0015" },
            new { Nrp = "3124600010", Password = "12345678", FullName = "User 0010" }
        };

        foreach (var seedUser in defaultUsers)
        {
            var existingUser = await userManager.Users.FirstOrDefaultAsync(u => u.Nrp == seedUser.Nrp);
            if (existingUser is null)
            {
                var user = new AppUser
                {
                    Nrp = seedUser.Nrp,
                    UserName = seedUser.Nrp,
                    FullName = seedUser.FullName
                };

                var create = await userManager.CreateAsync(user, seedUser.Password);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException($"Seed user {seedUser.Nrp} failed: {BuildIdentityErrors(create.Errors)}");
                }

                await userManager.AddToRoleAsync(user, "User");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(existingUser.FullName) || existingUser.FullName == existingUser.Nrp)
                {
                    existingUser.FullName = seedUser.FullName;
                    await userManager.UpdateAsync(existingUser);
                }

                await EnsurePasswordAndUnlockAsync(existingUser, seedUser.Password, $"user {seedUser.Nrp}");

                var isUser = await userManager.IsInRoleAsync(existingUser, "User");
                if (!isUser)
                    await userManager.AddToRoleAsync(existingUser, "User");
            }
        }
    }
}

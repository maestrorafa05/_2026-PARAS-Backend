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

        // 2) Seed admin default
        if (string.IsNullOrWhiteSpace(adminOpt.Nrp) || string.IsNullOrWhiteSpace(adminOpt.Password))
        {
            // jika NRP atau password admin kosong, skip seeding admin
            return;
        }

        var nrp = adminOpt.Nrp.Trim();
        var existing = await userManager.Users.FirstOrDefaultAsync(u => u.Nrp == nrp);

        if (existing is null)
        {
            var admin = new AppUser
            {
                Nrp = nrp,
                UserName = nrp,              
                FullName = adminOpt.FullName
            };

            var create = await userManager.CreateAsync(admin, adminOpt.Password);
            if (!create.Succeeded)
            {
                
                var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Seed admin failed: {msg}");
            }

            await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            var isAdmin = await userManager.IsInRoleAsync(existing, "Admin");
            if (!isAdmin)
                await userManager.AddToRoleAsync(existing, "Admin");
        }
    }
}

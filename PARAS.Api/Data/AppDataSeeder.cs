using Microsoft.EntityFrameworkCore;

namespace PARAS.Api.Data;

public static class AppDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParasDbContext>();

        // Seed room jika kosong
        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(
                new Domain.Entities.Room
                {
                    Code = "R-101",
                    Name = "Ruang 101",
                    Location = "Gedung A Lt.1",
                    Capacity = 30,
                    Facilities = "Proyektor, AC, Whiteboard",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Domain.Entities.Room
                {
                    Code = "R-202",
                    Name = "Ruang 202",
                    Location = "Gedung A Lt.2",
                    Capacity = 40,
                    Facilities = "TV, AC",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        }
    }
}

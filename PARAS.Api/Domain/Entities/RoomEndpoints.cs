using Microsoft.EntityFrameworkCore;
using PARAS.Api.Data;
using PARAS.Api.Domain.Entities;
using PARAS.Api.DTOs;

namespace PARAS.Api.Endpoints;

public static class RoomEndpoints
{
    // endpoint untuk mengelola data ruangan
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms").WithTags("Rooms");

        // endpoint untuk mendapatkan daftar semua ruangan (public)
        group.MapGet("/", async (ParasDbContext db) =>
        {
            // mengambil semua data ruangan dari database
            var rooms = await db.Rooms
                .AsNoTracking() 
                .OrderBy(r => r.Code)
                .Select(r => new RoomResponse(
                    r.Id, r.Code, r.Name, r.Location, r.Capacity, r.Facilities, r.IsActive, r.CreatedAt, r.UpdatedAt
                ))
                .ToListAsync();

            return Results.Ok(rooms);
        })
        .AllowAnonymous(); // explicit: boleh tanpa login

        // endpoint untuk mendapatkan detail ruangan berdasarkan id (public)
        group.MapGet("/{id:guid}", async (Guid id, ParasDbContext db) =>
        {
            // cari ruangan berdasarkan id
            var room = await db.Rooms
                .AsNoTracking() 
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room is null) return Results.NotFound();

            // return data ruangan dalam format RoomResponse
            return Results.Ok(new RoomResponse(
                room.Id, room.Code, room.Name, room.Location, room.Capacity, room.Facilities, room.IsActive, room.CreatedAt, room.UpdatedAt
            ));
        })
        .AllowAnonymous(); // explicit: boleh tanpa login

        // endpoint untuk membuat data ruangan baru (admin only)
        group.MapPost("/", async (CreateRoomRequest req, ParasDbContext db) =>
        {
            // validasi input data ruangan
            if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("Code dan Name tidak boleh kosong!");

            var code = req.Code.Trim();
            var name = req.Name.Trim();

            // validasi kapasitas harus lebih dari 0
            if (req.Capacity <= 0)
                return Results.BadRequest("Capacity harus lebih dari 0!");

            // cek apakah kode ruangan sudah digunakan
            var codeExists = await db.Rooms.AnyAsync(r => r.Code == code);
            if (codeExists)
                return Results.BadRequest($"Room code '{code}' sudah digunakan");

            // buat objek Room baru dari data request
            var room = new Room
            {
                Code = code,
                Name = name,
                Location = req.Location?.Trim(),
                Capacity = req.Capacity,
                Facilities = req.Facilities?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // simpan data ruangan ke database
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            // return hasil create
            return Results.Created($"/rooms/{room.Id}", new RoomResponse(
                room.Id, room.Code, room.Name, room.Location, room.Capacity, room.Facilities, room.IsActive, room.CreatedAt, room.UpdatedAt
            ));
        })
        .RequireAuthorization("AdminOnly"); 
        // endpoint untuk mengupdate data ruangan berdasarkan id (admin only)
        group.MapPut("/{id:guid}", async (Guid id, UpdateRoomRequest req, ParasDbContext db) =>
        {
            // cari ruangan berdasarkan id
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (room is null) return Results.NotFound();

            // validasi input data ruangan
            if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("Code dan Name tidak boleh kosong!");

            var code = req.Code.Trim();
            var name = req.Name.Trim();

            // validasi kapasitas harus lebih dari 0
            if (req.Capacity <= 0)
                return Results.BadRequest("Capacity harus lebih dari 0!");

            // cek apakah kode ruangan sudah digunakan oleh ruangan lain
            var codeExists = await db.Rooms.AnyAsync(r => r.Code == code && r.Id != id);
            if (codeExists)
                return Results.BadRequest($"Room code '{code}' sudah digunakan");

            // update data ruangan dengan data dari request
            room.Code = code;
            room.Name = name;
            room.Location = req.Location?.Trim();
            room.Capacity = req.Capacity;
            room.Facilities = req.Facilities?.Trim();
            room.IsActive = req.IsActive;
            room.UpdatedAt = DateTime.UtcNow;

            // simpan perubahan ke database
            await db.SaveChangesAsync();

            return Results.Ok(new RoomResponse(
                room.Id, room.Code, room.Name, room.Location, room.Capacity, room.Facilities, room.IsActive, room.CreatedAt, room.UpdatedAt
            ));
        })
        .RequireAuthorization("AdminOnly"); 

        // endpoint untuk menghapus data ruangan berdasarkan id (admin only)
        group.MapDelete("/{id:guid}", async (Guid id, ParasDbContext db) =>
        {
            // cari ruangan berdasarkan id
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (room is null) return Results.NotFound();

            // tandai ruangan sebagai tidak aktif (soft delete)
            room.IsActive = false;
            room.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly"); 

        return app;
    }
}

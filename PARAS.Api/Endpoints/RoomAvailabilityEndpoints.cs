using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Data;
using PARAS.Api.Domain.Enums;

namespace PARAS.Api.Endpoints;

public static class RoomAvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapRoomAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms").WithTags("Rooms");

        // get daftar ruangan yang tersedia untuk rentang waktu tertentu
        group.MapGet("/available", async (DateTime start, DateTime end, ParasDbContext db) =>
        {
            if (end <= start) return Results.BadRequest("End harus lebih besar dari Start.");

            // rooms aktif yang tidak punya loan overlap
            var availableRooms = await db.Rooms
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Where(r => !db.Loans.Any(l =>
                    l.RoomId == r.Id
                    && l.Status != LoanStatus.rejected
                    && l.Status != LoanStatus.cancelled
                    && start < l.EndTime
                    && end > l.StartTime
                ))
                .OrderBy(r => r.Code)
                .Select(r => new
                {
                    r.Id,
                    r.Code,
                    r.Name,
                    r.Location,
                    r.Capacity,
                    r.Facilities
                })
                .ToListAsync();

            return Results.Ok(availableRooms);
        });

        // get availability untuk satu ruangan tertentu
        group.MapGet("/{id:guid}/availability", async (Guid id, DateTime start, DateTime end, ParasDbContext db) =>
        {
            if (end <= start) return Results.BadRequest("End harus lebih besar dari Start.");

            var roomExists = await db.Rooms.AnyAsync(r => r.Id == id);
            if (!roomExists) return Results.NotFound("Room tidak ditemukan.");

            var conflicts = await db.Loans
                .AsNoTracking()
                .Where(l => l.RoomId == id
                    && l.Status != LoanStatus.rejected
                    && l.Status != LoanStatus.cancelled
                    && start < l.EndTime
                    && end > l.StartTime
                )
                .Select(l => new { l.Id, l.StartTime, l.EndTime, l.Status })
                .ToListAsync();

            return Results.Ok(new
            {
                roomId = id,
                start,
                end,
                isAvailable = conflicts.Count == 0,
                conflicts
            });
        });

        return app;
    }
}

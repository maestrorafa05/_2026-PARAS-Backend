using Microsoft.EntityFrameworkCore;
using PARAS.Api.Data;
using PARAS.Api.Domain.Entities;
using PARAS.Api.Domain.Enums;
using PARAS.Api.DTOs;

namespace PARAS.Api.Endpoints;

public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loans").WithTags("Loans");
        // endpoint untuk mendapatkan daftar semua peminjaman
        group.MapGet("/", async (ParasDbContext db) =>
        {      
            var loans = await db.Loans
                .AsNoTracking()
                .Include(l => l.Room)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new LoanResponse(
                    l.Id, l.RoomId, l.Room.Code, l.Room.Name, l.NamaPeminjam, l.NRP, l.StartTime, l.EndTime, l.Status, l.Notes, l.CreatedAt, l.UpdatedAt
                )).ToListAsync();

            return Results.Ok(loans);
        });
        // endpoint untuk mendapatkan detail peminjaman berdasarkan id
        group.MapGet("/{id:guid}", async (Guid id, ParasDbContext db) =>
        {
            var loan = await db.Loans
                .AsNoTracking()
                .Include(l => l.Room)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan is null) return Results.NotFound();

            return Results.Ok(new LoanResponse(
                loan.Id, loan.RoomId, loan.Room.Code, loan.Room.Name, loan.NamaPeminjam, loan.NRP, loan.StartTime, loan.EndTime, loan.Status, loan.Notes, loan.CreatedAt, loan.UpdatedAt
            ));
        });

        group.MapPost("/", async (CreateLoanRequest req, ParasDbContext db) =>
        {
            // validasi input data peminjaman
            if (string.IsNullOrWhiteSpace(req.NamaPeminjam))
                return Results.BadRequest("NamaPeminjam tidak boleh kosong!");
            if (string.IsNullOrWhiteSpace(req.NRP))
                return Results.BadRequest("NRP tidak boleh kosong!");
            // validasi waktu peminjaman
            if (req.StartTime >= req.EndTime)
                return Results.BadRequest("StartTime harus lebih awal dari EndTime!");
            // pengecekan ketersediaan ruangan untuk waktu yang diminta
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == req.RoomId);
            if (room is null)
                return Results.BadRequest($"Ruangan dengan id '{req.RoomId}' tidak ditemukan");
            // validasi status aktif ruangan
            if (!room.IsActive)
                return Results.BadRequest($"Ruangan '{room.Name}' tidak aktif dan tidak dapat dipinjam");
            // cek bentrok jadwal peminjaman dengan peminjaman lain yang sudah ada di database
            var conflict = await db.Loans.AnyAsync(l =>
                l.RoomId == req.RoomId &&
                l.Status != LoanStatus.rejected &&
                l.Status != LoanStatus.cancelled &&
                req.StartTime < l.EndTime &&
                req.EndTime > l.StartTime
            );
            // jika ada bentrok jadwal, maka peminjaman tidak bisa dibuat
            if (conflict)
                return Results.BadRequest("Jadwal bentrok, ruangan sudah dipinjam pada rentang waktu tersebut");
            
            var loan = new Loan
            {
                RoomId = req.RoomId,
                NamaPeminjam = req.NamaPeminjam.Trim(),
                NRP = req.NRP.Trim(),
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                Notes = req.Notes?.Trim(),
                UpdatedAt = DateTime.UtcNow
            };
            // simpan data peminjaman ke database
            await db.SaveChangesAsync();

            await db.Entry(loan).Reference(x => x.Room).LoadAsync();

            return Results.Ok(new LoanResponse(
                loan.Id, loan.RoomId, loan.Room.Code, loan.Room.Name, loan.NamaPeminjam, loan.NRP, loan.StartTime, loan.EndTime, loan.Status, loan.Notes, loan.CreatedAt, loan.UpdatedAt
            ));
        });


        group.MapDelete("/{id:guid}", async (Guid id, ParasDbContext db) =>
        {
            // cari peminjaman berdasarkan id
            var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound();

            // hanya peminjaman dengan status pending yang bisa dibatalkan
            if (loan.Status == LoanStatus.pending)
                return Results.Conflict("Peminjaman masih dalam status pending dan belum bisa dibatalkan");
            // hapus data peminjaman dari database
            db.Loans.Remove(loan);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}    
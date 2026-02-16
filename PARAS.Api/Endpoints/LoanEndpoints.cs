using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Data;
using PARAS.Api.Domain.Entities;
using PARAS.Api.Domain.Enums;
using PARAS.Api.DTOs;
using PARAS.Api.Services;
using PARAS.Api.Options;

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
                    l.Id, l.RoomId, l.Room.Code, l.Room.Name,
                    l.NamaPeminjam, l.NRP,
                    l.StartTime, l.EndTime,
                    l.Status, l.Notes,
                    l.CreatedAt, l.UpdatedAt
                ))
                .ToListAsync();

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
                loan.Id, loan.RoomId, loan.Room.Code, loan.Room.Name,
                loan.NamaPeminjam, loan.NRP,
                loan.StartTime, loan.EndTime,
                loan.Status, loan.Notes,
                loan.CreatedAt, loan.UpdatedAt
            ));
        });

        // endpoint untuk mendapatkan riwayat perubahan status peminjaman
        group.MapGet("/{id:guid}/history", async (Guid id, ParasDbContext db) =>
        {
            var exists = await db.Loans.AnyAsync(l => l.Id == id);
            if (!exists) return Results.NotFound("Loan tidak ditemukan.");

            var history = await db.LoanStatusHistories
                .AsNoTracking()
                .Where(h => h.LoanId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new LoanStatusHistoryResponse(
                    h.Id,
                    h.LoanId,
                    h.FromStatus,
                    h.ToStatus,
                    h.ChangedBy,
                    h.Comment,
                    h.ChangedAt
                ))
                .ToListAsync();

            return Results.Ok(history);
        });

        group.MapPost("/", async (CreateLoanRequest req, ParasDbContext db, LoanRulesValidator validator) =>
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

            var now = DateTime.Now; // local time (WIB) karena kamu dev lokal
            var errors = validator.Validate(req.StartTime, req.EndTime, now);
            if (errors.Count > 0)
                return Results.BadRequest(new { errors });

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
                return Results.Conflict("Jadwal bentrok, ruangan sudah dipinjam pada rentang waktu tersebut");

            var loan = new Loan
            {
                RoomId = req.RoomId,
                NamaPeminjam = req.NamaPeminjam.Trim(),
                NRP = req.NRP.Trim(),
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                Notes = req.Notes?.Trim(),

                // set default status & audit
                Status = LoanStatus.pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // simpan data peminjaman ke database
            db.Loans.Add(loan);
            await db.SaveChangesAsync();

            await db.Entry(loan).Reference(x => x.Room).LoadAsync();

            return Results.Created($"/loans/{loan.Id}", new LoanResponse(
                loan.Id, loan.RoomId, loan.Room.Code, loan.Room.Name,
                loan.NamaPeminjam, loan.NRP,
                loan.StartTime, loan.EndTime,
                loan.Status, loan.Notes,
                loan.CreatedAt, loan.UpdatedAt
            ));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ParasDbContext db) =>
        {
            // cari peminjaman berdasarkan id
            var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound();
            
            // hanya peminjaman dengan status pending atau approved yang bisa dibatalkan
            if (loan.Status is PARAS.Api.Domain.Enums.LoanStatus.rejected or PARAS.Api.Domain.Enums.LoanStatus.cancelled)
                return Results.Conflict("Loan sudah final, tidak bisa dibatalkan.");
            
            // ubah status peminjaman menjadi cancelled
            var from = loan.Status;
            var to = PARAS.Api.Domain.Enums.LoanStatus.cancelled;

            // pakai rules (Pending->Cancelled, Approved->Cancelled valid)
            if (!LoanStatusRules.IsValidTransition(from, to))
                return Results.Conflict($"Transisi status tidak valid: {from} -> {to}");

            loan.Status = to;
            loan.UpdatedAt = DateTime.UtcNow;

            db.LoanStatusHistories.Add(new LoanStatusHistory
            {
                LoanId = loan.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedBy = "system",
                Comment = "Cancelled via DELETE",
                ChangedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeLoanStatusRequest req, ParasDbContext db) =>
        {
            var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound("Loan tidak ditemukan.");

            // validasi input
            if (string.IsNullOrWhiteSpace(req.admin))
                return Results.BadRequest("admin wajib diisi.");

            var from = loan.Status;
            var to = req.ToStatus;

            if (from == to)
                return Results.BadRequest("Status baru sama dengan status saat ini.");

            // validasi transisi status
            if (!LoanStatusRules.IsValidTransition(from, to))
                return Results.Conflict($"Transisi status tidak valid: {from} -> {to}");

            // jika ingin mengubah status menjadi Approved, cek bentrok jadwal dengan peminjaman lain yang sudah Approved
            if (to == LoanStatus.approved){
                var conflict = await db.Loans.AnyAsync(l =>
                    l.Id != id
                    && l.RoomId == loan.RoomId
                    && l.Status == LoanStatus.approved
                    && loan.StartTime < l.EndTime
                    && loan.EndTime > l.StartTime
                );

                if (conflict)
                    return Results.Conflict("Tidak bisa approve: jadwal bentrok dengan loan Approved lain.");
            }

            // Update status peminjaman
            loan.Status = to;
            loan.UpdatedAt = DateTime.UtcNow;

            // simpan perubahan ke database (history)
            var history = new LoanStatusHistory
            {
                LoanId = loan.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedBy = req.admin.Trim(),
                Comment = req.Comment?.Trim(),
                ChangedAt = DateTime.UtcNow
            };

            // simpan riwayat perubahan status ke database
            db.LoanStatusHistories.Add(history);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                loanId = loan.Id,
                fromStatus = from,
                toStatus = to,
                changedAt = history.ChangedAt
            });
        });

        return app;
    }
}

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Auth;
using PARAS.Api.Data;
using PARAS.Api.Domain.Entities;
using PARAS.Api.Domain.Enums;
using PARAS.Api.DTOs;
using PARAS.Api.Services;

namespace PARAS.Api.Endpoints;

public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loans")
            .WithTags("Loans")
            .RequireAuthorization();

        // endpoint untuk mendapatkan daftar semua peminjaman (admin only)
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
        })
        .RequireAuthorization("AdminOnly");

        // endpoint untuk melihat peminjaman milik user yang sedang login
        group.MapGet("/mine", async (HttpContext ctx, ParasDbContext db, UserManager<AppUser> userManager) =>
        {
            // ambil userId dari JWT (sub) agar paling akurat
            var userId = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(guid.ToString());
            if (user is null) return Results.Unauthorized();

            var loans = await db.Loans
                .AsNoTracking()
                .Include(l => l.Room)
                .Where(l => l.NRP == user.Nrp)
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

        // endpoint untuk mendapatkan detail peminjaman berdasarkan id (admin atau pemilik)
        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, ParasDbContext db, UserManager<AppUser> userManager) =>
        {
            var loan = await db.Loans
                .AsNoTracking()
                .Include(l => l.Room)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan is null) return Results.NotFound();

            // cek akses: admin boleh, user hanya boleh akses miliknya
            var isAdmin = ctx.User.IsInRole("Admin");

            // ambil NRP user login (pakai sub -> DB, fallback claim "nrp")
            string? myNrp = ctx.User.FindFirst("nrp")?.Value;

            var userId = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!isAdmin && Guid.TryParse(userId, out var guid))
            {
                var user = await userManager.FindByIdAsync(guid.ToString());
                myNrp = user?.Nrp ?? myNrp;
            }

            if (!isAdmin && !string.Equals(loan.NRP, myNrp, StringComparison.OrdinalIgnoreCase))
                return Results.Forbid();

            return Results.Ok(new LoanResponse(
                loan.Id, loan.RoomId, loan.Room.Code, loan.Room.Name,
                loan.NamaPeminjam, loan.NRP,
                loan.StartTime, loan.EndTime,
                loan.Status, loan.Notes,
                loan.CreatedAt, loan.UpdatedAt
            ));
        });

        // endpoint untuk mendapatkan riwayat perubahan status peminjaman (admin atau pemilik)
        group.MapGet("/{id:guid}/history", async (Guid id, HttpContext ctx, ParasDbContext db, UserManager<AppUser> userManager) =>
        {
            // ambil loan dulu untuk cek ownership
            var loan = await db.Loans
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan is null) return Results.NotFound("Loan tidak ditemukan.");

            var isAdmin = ctx.User.IsInRole("Admin");

            // ambil NRP user login (pakai sub -> DB, fallback claim "nrp")
            string? myNrp = ctx.User.FindFirst("nrp")?.Value;

            var userId = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!isAdmin && Guid.TryParse(userId, out var guid))
            {
                var user = await userManager.FindByIdAsync(guid.ToString());
                myNrp = user?.Nrp ?? myNrp;
            }

            if (!isAdmin && !string.Equals(loan.NRP, myNrp, StringComparison.OrdinalIgnoreCase))
                return Results.Forbid();

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

        // endpoint untuk membuat peminjaman baru (user & admin)
        group.MapPost("/", async (
            CreateLoanRequest req,
            HttpContext ctx,
            ParasDbContext db,
            LoanRulesValidator validator,
            UserManager<AppUser> userManager) =>
        {
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

            // rules validator (jam operasional/durasi/min lead time, dll)
            var now = DateTime.Now; // local time (WIB) karena kamu dev lokal
            var errors = validator.Validate(req.StartTime, req.EndTime, now);
            if (errors.Count > 0)
                return Results.BadRequest(new { errors });

            // tentukan identitas peminjam:
            // - admin boleh create untuk user lain (pakai req.NRP & req.NamaPeminjam)
            // - user biasa: pakai NRP & FullName dari user login (anti impersonation)
            var isAdmin = ctx.User.IsInRole("Admin");

            string nrp;
            string nama;

            if (isAdmin)
            {
                // validasi input data peminjaman (khusus admin create atas nama orang lain)
                if (string.IsNullOrWhiteSpace(req.NamaPeminjam))
                    return Results.BadRequest("NamaPeminjam tidak boleh kosong!");
                if (string.IsNullOrWhiteSpace(req.NRP))
                    return Results.BadRequest("NRP tidak boleh kosong!");

                nrp = req.NRP.Trim();
                nama = req.NamaPeminjam.Trim();
            }
            else
            {
                // user biasa: ambil dari token (sub -> DB), fallback claim "nrp"
                var userId = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
                    return Results.Unauthorized();

                var user = await userManager.FindByIdAsync(guid.ToString());
                if (user is null) return Results.Unauthorized();

                nrp = user.Nrp.Trim();
                nama = (user.FullName ?? "User").Trim();
            }

            // cek bentrok jadwal peminjaman dengan peminjaman lain yang masih aktif (bukan rejected/cancelled)
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
                NamaPeminjam = nama,
                NRP = nrp,
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

        // endpoint untuk membatalkan peminjaman (admin atau pemilik)
        group.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, ParasDbContext db, UserManager<AppUser> userManager) =>
        {
            // cari peminjaman berdasarkan id
            var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound();

            // cek akses: admin boleh, user hanya boleh cancel miliknya
            var isAdmin = ctx.User.IsInRole("Admin");

            string? myNrp = ctx.User.FindFirst("nrp")?.Value;
            var userId = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!isAdmin && Guid.TryParse(userId, out var guid))
            {
                var user = await userManager.FindByIdAsync(guid.ToString());
                myNrp = user?.Nrp ?? myNrp;
            }

            if (!isAdmin && !string.Equals(loan.NRP, myNrp, StringComparison.OrdinalIgnoreCase))
                return Results.Forbid();

            // hanya peminjaman yang belum final yang bisa dibatalkan
            if (loan.Status is LoanStatus.rejected or LoanStatus.cancelled)
                return Results.Conflict("Loan sudah final, tidak bisa dibatalkan.");

            // ubah status peminjaman menjadi cancelled
            var from = loan.Status;
            var to = LoanStatus.cancelled;

            // pakai rules (Pending->Cancelled, Approved->Cancelled valid)
            if (!LoanStatusRules.IsValidTransition(from, to))
                return Results.Conflict($"Transisi status tidak valid: {from} -> {to}");

            loan.Status = to;
            loan.UpdatedAt = DateTime.UtcNow;

            // simpan riwayat perubahan status
            db.LoanStatusHistories.Add(new LoanStatusHistory
            {
                LoanId = loan.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedBy = isAdmin ? "admin" : (myNrp ?? "user"),
                Comment = "Cancelled via DELETE",
                ChangedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // endpoint untuk mengubah status peminjaman (admin only)
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
            if (to == LoanStatus.approved)
            {
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
        })
        .RequireAuthorization("AdminOnly");

        return app;
    }
}

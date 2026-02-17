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

        static string? GetNrp(System.Security.Claims.ClaimsPrincipal user)
        {
            // claim "nrp" kamu sudah ada di JWT (terlihat dari payload token)
            return user.FindFirst("nrp")?.Value
                   ?? user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                   ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value;
        }

        static bool IsAdmin(System.Security.Claims.ClaimsPrincipal user)
            => user.IsAdmin();

        // endpoint untuk mendapatkan daftar semua peminjaman
        // - Admin: lihat semua
        // - User: hanya lihat miliknya sendiri (NRP)
        group.MapGet("/", async (HttpContext ctx, ParasDbContext db) =>
        {
            var nrp = GetNrp(ctx.User);
            if (string.IsNullOrWhiteSpace(nrp))
                return Results.Unauthorized();

            var query = db.Loans
                .AsNoTracking()
                .Include(l => l.Room)
                .OrderByDescending(l => l.CreatedAt)
                .AsQueryable();

            if (!IsAdmin(ctx.User))
                query = query.Where(l => l.NRP == nrp);

            var loans = await query
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
        // - Admin: bebas
        // - User: hanya boleh akses loan miliknya
        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, ParasDbContext db) =>
        {
            var nrp = GetNrp(ctx.User);
            if (string.IsNullOrWhiteSpace(nrp))
                return Results.Unauthorized();

            var loan = await db.Loans
                .AsNoTracking()
                .Include(l => l.Room)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan is null) return Results.NotFound();

            if (!IsAdmin(ctx.User) && loan.NRP != nrp)
                return Results.Forbid();

            return Results.Ok(new LoanResponse(
                loan.Id, loan.RoomId, loan.Room.Code, loan.Room.Name,
                loan.NamaPeminjam, loan.NRP,
                loan.StartTime, loan.EndTime,
                loan.Status, loan.Notes,
                loan.CreatedAt, loan.UpdatedAt
            ));
        });

        // endpoint untuk mendapatkan riwayat perubahan status peminjaman
        // - Admin: bebas
        // - User: hanya loan miliknya
        group.MapGet("/{id:guid}/history", async (Guid id, HttpContext ctx, ParasDbContext db) =>
        {
            var nrp = GetNrp(ctx.User);
            if (string.IsNullOrWhiteSpace(nrp))
                return Results.Unauthorized();

            var loan = await db.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound("Loan tidak ditemukan.");

            if (!IsAdmin(ctx.User) && loan.NRP != nrp)
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

        // endpoint untuk membuat peminjaman (user create untuk dirinya sendiri)
        group.MapPost("/", async (
            CreateLoanRequest req,
            HttpContext ctx,
            ParasDbContext db,
            LoanRulesValidator validator,
            UserManager<AppUser> userManager) =>
        {
            // validasi identitas user dari JWT
            var nrpClaim = GetNrp(ctx.User);
            if (string.IsNullOrWhiteSpace(nrpClaim))
                return Results.Unauthorized();

            var userId = ctx.User.GetUserId();
            if (userId == Guid.Empty)
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Results.Unauthorized();

            // pastikan data user penting tidak null/kosong
            var userNrp = user.Nrp.Trim();
            if (string.IsNullOrWhiteSpace(userNrp))
                return Results.Problem("User NRP kosong di database.", statusCode: StatusCodes.Status500InternalServerError);

            // optional: cek konsistensi claim vs db
            if (!string.Equals(userNrp, nrpClaim, StringComparison.Ordinal))
                return Results.Unauthorized();

            var fullName = user.FullName.Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = userNrp; // fallback aman (atau ganti jadi Problem() kalau mau strict)

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

            // validasi aturan booking (jam operasi, min lead time, max duration, dll)
            var now = DateTime.Now; // local time (WIB) karena kamu dev lokal
            var errors = validator.Validate(req.StartTime, req.EndTime, now);
            if (errors.Count > 0)
                return Results.BadRequest(new { errors });

            // cek bentrok jadwal peminjaman dengan peminjaman lain yang belum final reject/cancel
            var conflict = await db.Loans.AnyAsync(l =>
                l.RoomId == req.RoomId &&
                l.Status != LoanStatus.rejected &&
                l.Status != LoanStatus.cancelled &&
                req.StartTime < l.EndTime &&
                req.EndTime > l.StartTime
            );

            if (conflict)
                return Results.Conflict("Jadwal bentrok, ruangan sudah dipinjam pada rentang waktu tersebut");

            var loan = new Loan
            {
                RoomId = req.RoomId,

                // identitas peminjam dari user login
                NamaPeminjam = fullName,
                NRP = userNrp,
                RequestedByUserId = userId,

                StartTime = req.StartTime,
                EndTime = req.EndTime,
                Notes = req.Notes?.Trim(),

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

        // endpoint untuk cancel loan:
        // - Admin boleh cancel siapa pun
        // - User hanya boleh cancel loan miliknya
        group.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, ParasDbContext db) =>
        {
            var nrp = GetNrp(ctx.User);
            if (string.IsNullOrWhiteSpace(nrp))
                return Results.Unauthorized();
            var userId = ctx.User.GetUserId();
            var changedByUserId = userId == Guid.Empty ? (Guid?)null : userId;

            var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound();

            if (!IsAdmin(ctx.User) && loan.NRP != nrp)
                return Results.Forbid();

            if (loan.Status is LoanStatus.rejected or LoanStatus.cancelled)
                return Results.Conflict("Loan sudah final, tidak bisa dibatalkan.");

            var from = loan.Status;
            var to = LoanStatus.cancelled;

            if (!LoanStatusRules.IsValidTransition(from, to))
                return Results.Conflict($"Transisi status tidak valid: {from} -> {to}");

            loan.Status = to;
            loan.UpdatedAt = DateTime.UtcNow;

            db.LoanStatusHistories.Add(new LoanStatusHistory
            {
                LoanId = loan.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedByUserId = changedByUserId,
                ChangedBy = nrp,
                Comment = "Cancelled via DELETE",
                ChangedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // endpoint untuk ubah status loan (approve/reject) => Admin only
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            ChangeLoanStatusRequest req,
            HttpContext ctx,
            ParasDbContext db) =>
        {
            var nrp = GetNrp(ctx.User);
            if (string.IsNullOrWhiteSpace(nrp))
                return Results.Unauthorized();
            var userId = ctx.User.GetUserId();
            var changedByUserId = userId == Guid.Empty ? (Guid?)null : userId;

            var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id);
            if (loan is null) return Results.NotFound("Loan tidak ditemukan.");

            var from = loan.Status;
            var to = req.ToStatus;

            if (from == to)
                return Results.BadRequest("Status baru sama dengan status saat ini.");

            // validasi transisi status
            if (!LoanStatusRules.IsValidTransition(from, to))
                return Results.Conflict($"Transisi status tidak valid: {from} -> {to}");

            // jika ingin approved, cek bentrok jadwal dengan loan approved lain
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

            // update status + audit
            loan.Status = to;
            loan.UpdatedAt = DateTime.UtcNow;

            // simpan history
            db.LoanStatusHistories.Add(new LoanStatusHistory
            {
                LoanId = loan.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedByUserId = changedByUserId,
                ChangedBy = nrp,
                Comment = req.Comment?.Trim(),
                ChangedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                loanId = loan.Id,
                fromStatus = from,
                toStatus = to
            });
        })
        .RequireAuthorization("AdminOnly");

        return app;
    }
}

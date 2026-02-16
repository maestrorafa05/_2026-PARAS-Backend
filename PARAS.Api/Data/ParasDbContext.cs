using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using PARAS.Api.Domain.Entities;

namespace PARAS.Api.Data;

public class ParasDbContext : DbContext
{
    public ParasDbContext(DbContextOptions<ParasDbContext> options) : base(options) {}

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanStatusHistory> LoanStatusHistories => Set<LoanStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ruangan memiliki banyak peminjaman
        modelBuilder.Entity<Room>(e =>
        {
            // konfigurasi untuk entitas Room
            e.ToTable("Rooms");
            e.HasKey(x => x.Id);
            
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Facilities).HasMaxLength(500);  

            // kode ruangan harus unique
            e.HasIndex(x => x.Code).IsUnique();
        });

        // peminjaman
        modelBuilder.Entity<Loan>(e =>
        {
            // konfigurasi untuk entitas Loan
            e.ToTable("Loans", tb =>
            {
                tb.HasCheckConstraint("CK_Loans_EndTime_After_StartTime", "[EndTime] > [StartTime]");
            });

            e.HasKey(x => x.Id);

            e.Property(x => x.NamaPeminjam).HasMaxLength(100).IsRequired();
            e.Property(x => x.NRP).HasMaxLength(20).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);

            e.HasIndex(x => new { x.RoomId, x.StartTime, x.EndTime, x.Status });

            e.HasOne(x => x.Room).WithMany(r => r.Loans).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        });

        // history perubahan status peminjaman
        modelBuilder.Entity<LoanStatusHistory>(e =>
        {
            // konfigurasi untuk entitas LoanStatusHistory
            e.ToTable("LoanStatusHistories");
            e.HasKey(x => x.Id);
            // konfigurasi untuk properti ChangedBy dan Comment dengan panjang maksimal
            e.Property(x => x.ChangedBy).HasMaxLength(100);
            e.Property(x => x.Comment).HasMaxLength(300);

            e.HasIndex(x => new { x.LoanId, x.ChangedAt });
            e.HasOne(x => x.Loan).WithMany(l => l.StatusHistories).HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
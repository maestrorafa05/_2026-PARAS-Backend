using PARAS.Api.Domain.Enums;

namespace PARAS.Api.Domain.Entities;

public class Loan
{
    public Guid Id {get; set;} = Guid.NewGuid();

    // FK
    public Guid RoomId {get; set;}
    public Room Room {get; set;} = default!;

    // Data peminjaman
    public string NamaPeminjam {get; set;} = default!;
    public string NRP {get; set;} = default!;
    public DateTime StartTime {get; set;}
    public DateTime EndTime {get; set;}

    public LoanStatus Status {get; set;} = LoanStatus.pending;
    public string? Notes {get; set;}

    // timestamps untuk pencatatan waktu pembuatan dan pembaruan data
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
    // untuk mencatat siapa yang membuat/mengubah data peminjaman 
    public Guid RequestedByUserId { get; set; } 

    // riwayat perubahan status peminjaman
    public List<LoanStatusHistory> StatusHistories {get; set;} = new();
}
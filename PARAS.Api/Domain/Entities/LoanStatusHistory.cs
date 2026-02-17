using PARAS.Api.Domain.Enums;

namespace PARAS.Api.Domain.Entities;

public class LoanStatusHistory
{
    // entitas untuk melacak perubahan status peminjaman
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid LoanId {get; set;}
    public Loan Loan {get; set;} = default!;

    // status sebelum dan sesudah perubahan
    public LoanStatus FromStatus {get; set;}
    public LoanStatus ToStatus {get; set;}

    // informasi tambahan untuk pelacakan perubahan status
    public Guid? ChangedByUserId { get; set; }
    public string ChangedBy {get; set;} = "System";
    public string? Comment {get; set;}
    public DateTime ChangedAt {get; set;} = DateTime.UtcNow;
   
}

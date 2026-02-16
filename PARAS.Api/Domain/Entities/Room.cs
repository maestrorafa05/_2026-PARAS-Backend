using PARAS.Api.Domain.Enums;

namespace PARAS.Api.Domain.Entities;

public class Room
{
    public Guid Id {get; set;} = Guid.NewGuid();
 
    // kode unik untuk setiap ruangan
    public string Code {get; set;} = default!;
    // nama ruangan
    public string Name {get; set;} = default!;
    // lokasi ruangan
    public string? Location {get; set;}
    // kapasitas maksimal ruangan
    public int Capacity {get; set;}
    // fasilitas ruangan
    public string? Facilities {get; set;}
    // status aktif ruangan
    public bool IsActive {get; set;} = true;

    
    //
    public List<Loan> Loans {get; set;} = new();
}
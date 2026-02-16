namespace PARAS.Api.Options;

public class BookingRulesOptions
{
    // aturan peminjaman ruangan
    public TimeOnly OpenTime { get; set; } = new(7, 0);
    public TimeOnly CloseTime { get; set; } = new(20, 0);

    // durasi peminjaman minimum dan maksimum dalam menit
    public int MinDurationMinutes { get; set; } = 30;
    public int MaxDurationMinutes { get; set; } = 240; // 4 jam

    // batas waktu booking
    public int MaxAdvanceDays { get; set; } = 30;      
    public int MinLeadMinutes { get; set; } = 10;     
    // buffer waktu antara peminjaman untuk mencegah overlap
    public int BufferMinutesBetweenBookings { get; set; } = 0; 
    public bool AllowWeekend { get; set; } = true;
}

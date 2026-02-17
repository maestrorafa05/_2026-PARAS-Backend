namespace PARAS.Api.Options;

public class AdminSeedOptions
{
    // konfigurasi untuk seeding admin awal
    public string Nrp { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? FullName { get; set; }
}

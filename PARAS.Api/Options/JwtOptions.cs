namespace PARAS.Api.Options;

public class JwtOptions
{
    // konfigurasi JWT untuk autentikasi
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string Key { get; set; } = default!;
    public int ExpiresMinutes { get; set; } = 120;
}

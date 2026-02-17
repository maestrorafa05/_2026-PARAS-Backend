using Microsoft.AspNetCore.Identity;

namespace PARAS.Api.Auth;

public class AppUser : IdentityUser<Guid>
{
    public string Nrp { get; set; } = default!;
    public string FullName { get; set; } = default!;
}

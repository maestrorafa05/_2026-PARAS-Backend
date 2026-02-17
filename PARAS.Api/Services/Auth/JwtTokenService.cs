using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PARAS.Api.Auth;
using PARAS.Api.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PARAS.Api.Services.Auth;

public class JwtTokenService
{
    private readonly JwtOptions _jwt;
    private readonly UserManager<AppUser> _userManager;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions, UserManager<AppUser> userManager)
    {
        _jwt = jwtOptions.Value;
        _userManager = userManager;
    }

    public async Task<(string token, int expiresMinutes, string[] roles)> CreateTokenAsync(AppUser user)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        // claims utama
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Nrp),
            new("nrp", user.Nrp)
        };

        // role claims (penting untuk [Authorize(Roles="Admin")])
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
            signingCredentials: creds
        );

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenStr, _jwt.ExpiresMinutes, roles);
    }
}

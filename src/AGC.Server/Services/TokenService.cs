using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AGC.Server.Configuration;
using AGC.Server.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AGC.Server.Services;

public sealed class TokenService(AppOptions options) : ITokenService
{
    public const string IsOwnerClaimType = "agc:is_owner";

    public string IssueToken(User user, bool isOwner)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(options.JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(IsOwnerClaimType, isOwner ? "true" : "false"),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

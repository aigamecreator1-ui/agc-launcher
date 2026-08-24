using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AGC.Server.Services;

namespace AGC.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string RequireUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated request had no subject claim.");

    public static bool IsOwner(this ClaimsPrincipal user) =>
        user.FindFirst(TokenService.IsOwnerClaimType)?.Value == "true";
}

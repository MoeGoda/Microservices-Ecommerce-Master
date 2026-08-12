using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Common.Security
{
    // Extracted here the moment a SECOND caller needed to sign a JWT.
    // Identity.Infrastructure's JwtTokenGenerator (A1) issues a token for
    // a signed-in User; Step C2 adds POS's own need to sign a short-lived
    // token representing the POS *service itself*, calling Warehouse.API
    // service-to-service. Both are the identical
    // SymmetricSecurityKey/SigningCredentials/JwtSecurityToken construction
    // — copy-pasting it a second time is exactly the kind of
    // security-sensitive duplication that drifts (one call site tweaks the
    // signing algorithm or a claim convention and the other doesn't). One
    // implementation, called by both.
    public static class JwtTokenFactory
    {
        public static string CreateToken(JwtSettings settings, IEnumerable<Claim> claims, DateTime expiresAtUtc)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

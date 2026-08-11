using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Contracts.Infrastructure;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

            // These three claims are the whole point: every downstream
            // service (Warehouse, POS, Reporting, ...) trusts the gateway's
            // JWT validation and reads ClaimTypes.Role straight off the
            // token to authorize [Authorize(Roles = "...")] — no service
            // ever calls back into Identity.API to ask "is this user an
            // Admin?". That round trip is exactly what a signed JWT avoids.
            // ClaimTypes.Name (not the JWT-standard "sub") on purpose: the
            // JwtBearer middleware's default NameClaimType is ClaimTypes.Name,
            // so this is what makes User.Identity.Name resolve downstream
            // without extra configuration. Mixing in JwtRegisteredClaimNames.Sub
            // here too would collide, because the token handler's default
            // inbound claim map already rewrites "sub" to ClaimTypes.NameIdentifier.
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
        }
    }
}

using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Infrastructure
{
    public interface IJwtTokenGenerator
    {
        // Returns the signed token string plus its UTC expiry, so the caller
        // can tell the client exactly when to expect a 401 and re-login.
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
    }
}

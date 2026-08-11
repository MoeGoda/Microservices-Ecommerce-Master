using Identity.Application.Contracts.Infrastructure;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Security
{
    // Thin wrapper around ASP.NET Core's own PasswordHasher<T> — it already
    // implements PBKDF2 with a per-call random salt and a safe number of
    // iterations, reviewed by Microsoft's security team. There's no good
    // reason to hand-roll this: password hashing is one of the few areas
    // where "write it yourself" is a real risk, not just extra work.
    public class PasswordHasher : IPasswordHasher
    {
        private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();

        public string Hash(User user, string plainPassword)
        {
            return _hasher.HashPassword(user, plainPassword);
        }

        public bool Verify(User user, string hash, string plainPassword)
        {
            var result = _hasher.VerifyHashedPassword(user, hash, plainPassword);
            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}

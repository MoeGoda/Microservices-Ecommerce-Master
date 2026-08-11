using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Infrastructure
{
    // Abstracts *how* passwords get hashed/verified so the command handlers
    // below stay crypto-agnostic. Infrastructure will implement this with
    // ASP.NET Core's PasswordHasher<User> (PBKDF2), but Application never
    // imports that library directly.
    public interface IPasswordHasher
    {
        string Hash(User user, string plainPassword);
        bool Verify(User user, string hash, string plainPassword);
    }
}

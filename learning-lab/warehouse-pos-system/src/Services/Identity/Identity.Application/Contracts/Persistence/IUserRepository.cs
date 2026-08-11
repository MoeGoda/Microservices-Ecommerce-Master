using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Persistence
{
    // The Application layer only ever depends on this interface, never on
    // EF Core or SqlServer directly. Infrastructure (a different project)
    // implements it. That's Dependency Inversion: the "policy" (business
    // rules in Application) doesn't depend on the "detail" (how data is
    // stored) — the detail depends on the policy's contract instead.
    public interface IUserRepository
    {
        Task<User?> GetByUserName(string userName);
        Task<User?> GetByEmail(string email);
        Task<bool> UserNameExists(string userName);
        Task<User> AddAsync(User user);
    }
}

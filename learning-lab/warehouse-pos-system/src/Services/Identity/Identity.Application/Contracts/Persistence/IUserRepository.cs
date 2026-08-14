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

        // H — user management screen additions. GetByIdAsync/SaveChangesAsync
        // exist specifically for SetUserActiveCommand: it loads the tracked
        // entity, flips IsActive, and needs an explicit save — there's no
        // "UpdateAsync" because EF Core's change tracker already knows about
        // any entity GetByIdAsync returns, the same reasoning every other
        // service's mutation handlers already follow.
        Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(int page, int pageSize);
        Task<User?> GetByIdAsync(int id);
        Task SaveChangesAsync();
    }
}

using Identity.Application.Contracts.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IdentityContext _context;

        public UserRepository(IdentityContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUserName(string userName)
        {
            // Include(Role) because LoginCommandHandler needs user.Role.Name
            // for the JWT claim and the AuthResponse — without it, EF Core's
            // lazy-loading-off-by-default would leave Role null and throw.
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UserNameExists(string userName)
        {
            return await _context.Users.AnyAsync(u => u.UserName == userName);
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(int page, int pageSize)
        {
            var query = _context.Users.Include(u => u.Role).OrderBy(u => u.UserName);
            var totalCount = await query.CountAsync();
            var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (users, totalCount);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

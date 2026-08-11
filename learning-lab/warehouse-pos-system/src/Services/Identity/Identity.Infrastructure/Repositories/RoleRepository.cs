using Identity.Application.Contracts.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IdentityContext _context;

        public RoleRepository(IdentityContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByName(string name)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }
    }
}

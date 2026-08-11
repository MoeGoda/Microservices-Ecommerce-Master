using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Persistence
{
    public interface IRoleRepository
    {
        Task<Role?> GetByName(string name);
    }
}

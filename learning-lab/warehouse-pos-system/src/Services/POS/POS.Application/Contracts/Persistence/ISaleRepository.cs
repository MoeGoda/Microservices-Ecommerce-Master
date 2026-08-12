using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface ISaleRepository
    {
        Task<Sale?> GetById(int id);
        Task<Sale> AddAsync(Sale sale);
        Task UpdateAsync(Sale sale);
    }
}

using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface ISaleLineRepository
    {
        Task<SaleLine?> GetById(int id);
        Task<IEnumerable<SaleLine>> GetBySale(int saleId);
        Task<SaleLine> AddAsync(SaleLine saleLine);
        Task DeleteAsync(SaleLine saleLine);
    }
}

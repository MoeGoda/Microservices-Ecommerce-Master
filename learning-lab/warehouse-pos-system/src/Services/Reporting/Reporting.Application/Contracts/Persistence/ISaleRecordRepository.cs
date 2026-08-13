using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface ISaleRecordRepository
    {
        Task<bool> ExistsForSale(int saleId);
        Task<SaleRecord> AddAsync(SaleRecord record);
        Task<IEnumerable<SaleRecord>> GetAll();
    }
}

using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface ISaleLineRecordRepository
    {
        Task<SaleLineRecord> AddAsync(SaleLineRecord record);
    }
}

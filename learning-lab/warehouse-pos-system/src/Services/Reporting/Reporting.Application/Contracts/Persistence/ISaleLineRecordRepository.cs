using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface ISaleLineRecordRepository
    {
        Task<SaleLineRecord> AddAsync(SaleLineRecord record);

        // Same "GROUP BY, return the aggregate DTO directly" reasoning as
        // ISaleRecordRepository.GetSalesByDay — ordered by TotalRevenue
        // descending, capped at `take` rows in the database, not after
        // pulling every SaleLineRecord into memory first.
        Task<IEnumerable<TopSellingItemDto>> GetTopSellingItems(int take);
    }
}

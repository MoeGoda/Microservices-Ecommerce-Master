using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface ISaleRecordRepository
    {
        Task<bool> ExistsForSale(int saleId);
        Task<SaleRecord> AddAsync(SaleRecord record);
        Task<IEnumerable<SaleRecord>> GetAll();

        // A GROUP BY, not an entity fetch — returns the aggregation DTO
        // directly rather than forcing the query handler to re-aggregate
        // every SaleRecord in memory. SalesByDayDto IS the shape of one
        // result row here; there's no separate "entity" underneath a
        // GROUP BY to map from the way GetAll()'s rows map from SaleRecord.
        Task<IEnumerable<SalesByDayDto>> GetSalesByDay();
    }
}

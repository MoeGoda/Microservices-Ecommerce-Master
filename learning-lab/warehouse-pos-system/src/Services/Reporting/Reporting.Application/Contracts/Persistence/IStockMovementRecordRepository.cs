using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface IStockMovementRecordRepository
    {
        Task<StockMovementRecord> AddAsync(StockMovementRecord record);

        // Newest first, optionally narrowed by date range/item/location —
        // the one report this ledger exists for. Unbounded like
        // SaleRecord's own GetPaged: this table only grows, so paging is
        // not optional the way it is for GetSalesByDay's small aggregate.
        Task<(IEnumerable<StockMovementRecord> Records, int TotalCount)> GetPaged(
            int page,
            int pageSize,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? itemId,
            int? locationId);
    }
}

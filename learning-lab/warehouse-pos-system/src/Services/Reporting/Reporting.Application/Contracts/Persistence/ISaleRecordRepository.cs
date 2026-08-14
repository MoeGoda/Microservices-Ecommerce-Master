using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface ISaleRecordRepository
    {
        Task<bool> ExistsForSale(int saleId);
        Task<SaleRecord> AddAsync(SaleRecord record);

        // F1 — replaces the old unbounded GetAll(): this is a raw,
        // ever-growing table dump with no natural upper bound. Returns
        // the total row count alongside the page, same idiom as
        // Warehouse's own IItemRepository.GetPaged.
        Task<(IEnumerable<SaleRecord> Records, int TotalCount)> GetPaged(int page, int pageSize);

        // Tracked, unlike GetAll's AsNoTracking rows — IngestSaleReturnedCommandHandler
        // mutates the row it gets back and relies on the change being
        // picked up by the same SaveChangesAsync call.
        Task<SaleRecord?> GetBySaleId(int saleId);
        Task UpdateAsync(SaleRecord record);

        // A GROUP BY, not an entity fetch — returns the aggregation DTO
        // directly rather than forcing the query handler to re-aggregate
        // every SaleRecord in memory. SalesByDayDto IS the shape of one
        // result row here; there's no separate "entity" underneath a
        // GROUP BY to map from the way GetAll()'s rows map from SaleRecord.
        Task<IEnumerable<SalesByDayDto>> GetSalesByDay();

        // J — the "payment operations by date" ledger: unlike GetPaged
        // above (a raw, unfiltered dump for ReadModelsController) this
        // one takes the date range the report is actually filtered by,
        // and — unlike GetSalesByDay — does NOT exclude returned sales;
        // see SalesLedgerEntryDto's own comment on why.
        Task<(IEnumerable<SaleRecord> Records, int TotalCount)> GetLedgerPaged(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc);

        // A GROUP BY, same idiom as GetSalesByDay — CashierPerformanceDto
        // IS the shape of one result row.
        Task<IEnumerable<CashierPerformanceDto>> GetCashierPerformance(DateTime? fromUtc, DateTime? toUtc);
    }
}

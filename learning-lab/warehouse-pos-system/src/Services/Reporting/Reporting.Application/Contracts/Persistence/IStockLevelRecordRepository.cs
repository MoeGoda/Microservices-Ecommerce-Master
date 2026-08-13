using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface IStockLevelRecordRepository
    {
        Task<StockLevelRecord?> GetByItemAndLocation(int itemId, int locationId);
        Task<StockLevelRecord> AddAsync(StockLevelRecord record);
        Task UpdateAsync(StockLevelRecord record);
        Task<IEnumerable<StockLevelRecord>> GetAll();

        // A plain filter, not an aggregation — unlike the two GROUP BY
        // methods on the other repositories, "low stock" is just
        // QuantityOnHand <= ReorderThreshold, so this stays an entity
        // fetch (GetLowStockQueryHandler maps to StockLevelRecordDto
        // itself, same as GetAll()'s own callers do).
        Task<IEnumerable<StockLevelRecord>> GetLowStock();
    }
}

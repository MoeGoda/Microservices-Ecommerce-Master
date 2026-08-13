using Reporting.Domain.Entities;

namespace Reporting.Application.Contracts.Persistence
{
    public interface IStockLevelRecordRepository
    {
        Task<StockLevelRecord?> GetByItemAndLocation(int itemId, int locationId);
        Task<StockLevelRecord> AddAsync(StockLevelRecord record);
        Task UpdateAsync(StockLevelRecord record);
        Task<IEnumerable<StockLevelRecord>> GetAll();
    }
}

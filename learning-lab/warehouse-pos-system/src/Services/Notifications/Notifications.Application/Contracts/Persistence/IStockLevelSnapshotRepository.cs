using Notifications.Domain.Entities;

namespace Notifications.Application.Contracts.Persistence
{
    public interface IStockLevelSnapshotRepository
    {
        Task<StockLevelSnapshot?> GetByItemAndLocation(int itemId, int locationId);
        Task<StockLevelSnapshot> AddAsync(StockLevelSnapshot snapshot);
        Task UpdateAsync(StockLevelSnapshot snapshot);
    }
}

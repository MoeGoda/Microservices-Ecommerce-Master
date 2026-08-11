using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IStockTransactionRepository
    {
        Task<StockTransaction> AddAsync(StockTransaction transaction);
        Task<IEnumerable<StockTransaction>> GetByItem(int itemId);
    }
}

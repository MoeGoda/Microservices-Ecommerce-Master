using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IStockLevelRepository
    {
        Task<StockLevel?> GetByItemAndLocation(int itemId, int locationId);
        Task<IEnumerable<StockLevel>> GetByItem(int itemId);
        Task<StockLevel> AddAsync(StockLevel stockLevel);

        // Deliberately no "AdjustQuantity" or "Upsert" method here — that's
        // a business operation (find-or-create a StockLevel AND write a
        // matching StockTransaction, atomically), not a persistence
        // primitive. It belongs in Step B2's command handler, which is the
        // one place that should decide when a StockLevel gets created vs.
        // updated and what audit trail that produces.
    }
}

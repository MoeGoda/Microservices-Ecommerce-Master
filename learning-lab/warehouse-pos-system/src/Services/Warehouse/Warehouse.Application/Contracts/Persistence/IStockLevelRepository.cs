using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IStockLevelRepository
    {
        Task<StockLevel?> GetByItemAndLocation(int itemId, int locationId);
        Task<IEnumerable<StockLevel>> GetByItem(int itemId);
        Task<StockLevel> AddAsync(StockLevel stockLevel);
        Task UpdateAsync(StockLevel stockLevel);

        // J — a GROUP BY, not an entity fetch, same idiom as Reporting's
        // ISaleRecordRepository.GetSalesByDay: the aggregation DTO IS the
        // shape of one result row; there's no separate StockLevel graph
        // to map from the way GetByItem's rows map 1:1 from the entity.
        Task<IEnumerable<InventoryValuationLineDto>> GetInventoryValuation();

        // Deliberately no "AdjustQuantity" or "Upsert" method here — that's
        // a business operation (find-or-create a StockLevel AND write a
        // matching StockTransaction, atomically), not a persistence
        // primitive. It belongs in Step B2's command handler, which is the
        // one place that should decide when a StockLevel gets created vs.
        // updated and what audit trail that produces.
    }
}

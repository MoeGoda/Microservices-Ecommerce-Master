using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IItemPriceHistoryRepository
    {
        // Stages only — see IUnitOfWork. UpdateItemPriceCommand commits
        // this together with the Item's own UnitPrice change.
        Task<ItemPriceHistory> AddAsync(ItemPriceHistory history);

        Task<IEnumerable<ItemPriceHistory>> GetByItem(int itemId);
    }
}

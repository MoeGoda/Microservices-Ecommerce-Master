using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IItemRepository
    {
        Task<Item?> GetById(int id);
        Task<Item?> GetBySku(string sku);
        Task<bool> SkuExists(string sku);
        Task<IEnumerable<Item>> GetAll();
        Task<Item> AddAsync(Item item);
    }
}

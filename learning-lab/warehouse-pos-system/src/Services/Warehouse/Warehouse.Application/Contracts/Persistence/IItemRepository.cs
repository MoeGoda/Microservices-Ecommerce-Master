using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IItemRepository
    {
        Task<Item?> GetById(int id);
        Task<Item?> GetBySku(string sku);
        Task<bool> SkuExists(string sku);
        Task<IEnumerable<Item>> GetAll();

        // Every pack/variant Item pointing at this one via ParentItemId —
        // e.g. the base "Water 500ml" Item's variants would include
        // "Water 500ml - Pack of 6."
        Task<IEnumerable<Item>> GetVariants(int parentItemId);

        Task<Item> AddAsync(Item item);

        // Stages only — see IUnitOfWork. So far only UpdateItemPriceCommand
        // (C5) needs this; every earlier command that mutates an Item
        // (AddItemBarcode/AddItemUnit) only touched a related entity, not
        // the Item itself.
        Task UpdateAsync(Item item);
    }
}

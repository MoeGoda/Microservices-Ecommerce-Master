using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IItemUnitRepository
    {
        Task<IEnumerable<ItemUnit>> GetByItem(int itemId);
        Task<ItemUnit?> GetByItemAndUnit(int itemId, int unitOfMeasureId);
        Task<ItemUnit> AddAsync(ItemUnit itemUnit);
    }
}

using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class ItemUnitRepository : IItemUnitRepository
    {
        private readonly WarehouseContext _context;

        public ItemUnitRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ItemUnit>> GetByItem(int itemId)
        {
            return await _context.ItemUnits
                .Include(u => u.UnitOfMeasure)
                .Where(u => u.ItemId == itemId)
                .ToListAsync();
        }

        public async Task<ItemUnit?> GetByItemAndUnit(int itemId, int unitOfMeasureId)
        {
            return await _context.ItemUnits
                .Include(u => u.UnitOfMeasure)
                .FirstOrDefaultAsync(u => u.ItemId == itemId && u.UnitOfMeasureId == unitOfMeasureId);
        }

        public async Task<ItemUnit> AddAsync(ItemUnit itemUnit)
        {
            await _context.ItemUnits.AddAsync(itemUnit);
            await _context.SaveChangesAsync();
            return itemUnit;
        }
    }
}

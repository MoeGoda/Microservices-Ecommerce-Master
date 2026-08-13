using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class ItemPriceHistoryRepository : IItemPriceHistoryRepository
    {
        private readonly WarehouseContext _context;

        public ItemPriceHistoryRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<ItemPriceHistory> AddAsync(ItemPriceHistory history)
        {
            await _context.ItemPriceHistories.AddAsync(history);
            return history;
        }

        public async Task<IEnumerable<ItemPriceHistory>> GetByItem(int itemId)
        {
            return await _context.ItemPriceHistories
                .Where(h => h.ItemId == itemId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}

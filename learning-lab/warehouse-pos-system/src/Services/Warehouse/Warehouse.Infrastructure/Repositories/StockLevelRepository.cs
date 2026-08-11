using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class StockLevelRepository : IStockLevelRepository
    {
        private readonly WarehouseContext _context;

        public StockLevelRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<StockLevel?> GetByItemAndLocation(int itemId, int locationId)
        {
            return await _context.StockLevels
                .FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId);
        }

        public async Task<IEnumerable<StockLevel>> GetByItem(int itemId)
        {
            return await _context.StockLevels
                .Include(s => s.Location)
                .Where(s => s.ItemId == itemId)
                .ToListAsync();
        }

        public async Task<StockLevel> AddAsync(StockLevel stockLevel)
        {
            await _context.StockLevels.AddAsync(stockLevel);
            await _context.SaveChangesAsync();
            return stockLevel;
        }
    }
}

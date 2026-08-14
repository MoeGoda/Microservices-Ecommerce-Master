using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
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
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId);
        }

        public async Task<IEnumerable<StockLevel>> GetByItem(int itemId)
        {
            return await _context.StockLevels
                .Include(s => s.Location)
                .Where(s => s.ItemId == itemId)
                .ToListAsync();
        }

        // Stages only — see IUnitOfWork. Receiving/adjusting stock needs
        // this AND a StockTransaction insert to commit together.
        public async Task<StockLevel> AddAsync(StockLevel stockLevel)
        {
            await _context.StockLevels.AddAsync(stockLevel);
            return stockLevel;
        }

        public Task UpdateAsync(StockLevel stockLevel)
        {
            _context.StockLevels.Update(stockLevel);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<InventoryValuationLineDto>> GetInventoryValuation()
        {
            return await _context.StockLevels
                .GroupBy(s => new
                {
                    s.ItemId,
                    Sku = s.Item.Sku,
                    ItemName = s.Item.Name,
                    CategoryName = s.Item.Category.Name,
                    UnitPrice = s.Item.UnitPrice,
                })
                .Select(g => new InventoryValuationLineDto
                {
                    ItemId = g.Key.ItemId,
                    Sku = g.Key.Sku,
                    ItemName = g.Key.ItemName,
                    CategoryName = g.Key.CategoryName,
                    TotalQuantityOnHand = g.Sum(s => s.QuantityOnHand),
                    UnitPrice = g.Key.UnitPrice,
                    TotalValue = g.Sum(s => s.QuantityOnHand) * g.Key.UnitPrice,
                })
                .OrderBy(l => l.ItemName)
                .ToListAsync();
        }
    }
}

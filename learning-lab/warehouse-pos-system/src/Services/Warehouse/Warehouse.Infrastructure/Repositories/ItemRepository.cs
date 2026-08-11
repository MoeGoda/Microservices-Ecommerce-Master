using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly WarehouseContext _context;

        public ItemRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<Item?> GetById(int id)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.BaseUnitOfMeasure)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Item?> GetBySku(string sku)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.BaseUnitOfMeasure)
                .FirstOrDefaultAsync(i => i.Sku == sku);
        }

        public async Task<bool> SkuExists(string sku)
        {
            return await _context.Items.AnyAsync(i => i.Sku == sku);
        }

        public async Task<IEnumerable<Item>> GetAll()
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.BaseUnitOfMeasure)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<Item> AddAsync(Item item)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }
    }
}

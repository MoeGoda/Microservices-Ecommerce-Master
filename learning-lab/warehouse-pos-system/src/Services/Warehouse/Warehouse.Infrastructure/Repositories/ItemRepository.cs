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
            return await _context.Items.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Item?> GetByBarcode(string barcode)
        {
            return await _context.Items.Include(i => i.Category).FirstOrDefaultAsync(i => i.Barcode == barcode);
        }

        public async Task<bool> BarcodeExists(string barcode)
        {
            return await _context.Items.AnyAsync(i => i.Barcode == barcode);
        }

        public async Task<IEnumerable<Item>> GetAll()
        {
            return await _context.Items.Include(i => i.Category).OrderBy(i => i.Name).ToListAsync();
        }

        public async Task<Item> AddAsync(Item item)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }
    }
}

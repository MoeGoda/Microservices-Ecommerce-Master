using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class ItemBarcodeRepository : IItemBarcodeRepository
    {
        private readonly WarehouseContext _context;

        public ItemBarcodeRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<ItemBarcode?> GetByBarcode(string barcode)
        {
            return await _context.ItemBarcodes
                .Include(b => b.Item)
                    .ThenInclude(i => i.Category)
                .Include(b => b.Item)
                    .ThenInclude(i => i.BaseUnitOfMeasure)
                .FirstOrDefaultAsync(b => b.Barcode == barcode);
        }

        public async Task<bool> BarcodeExists(string barcode)
        {
            return await _context.ItemBarcodes.AnyAsync(b => b.Barcode == barcode);
        }

        public async Task<IEnumerable<ItemBarcode>> GetByItem(int itemId)
        {
            return await _context.ItemBarcodes.Where(b => b.ItemId == itemId).ToListAsync();
        }

        public async Task<ItemBarcode?> GetPrimary(int itemId)
        {
            return await _context.ItemBarcodes.FirstOrDefaultAsync(b => b.ItemId == itemId && b.IsPrimary);
        }

        // Stages the insert only — does not call SaveChangesAsync. See
        // IUnitOfWork: the caller decides when (and what else) commits
        // together with this.
        public async Task<ItemBarcode> AddAsync(ItemBarcode itemBarcode)
        {
            await _context.ItemBarcodes.AddAsync(itemBarcode);
            return itemBarcode;
        }

        public Task UpdateAsync(ItemBarcode itemBarcode)
        {
            _context.ItemBarcodes.Update(itemBarcode);
            return Task.CompletedTask;
        }
    }
}

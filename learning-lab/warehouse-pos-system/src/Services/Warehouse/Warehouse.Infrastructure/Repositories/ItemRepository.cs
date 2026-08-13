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

        public async Task<(IEnumerable<Item> Items, int TotalCount)> GetPaged(int page, int pageSize)
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.BaseUnitOfMeasure)
                .OrderBy(i => i.Name);

            // Two round trips (count, then page) rather than one clever
            // query that returns both — EF Core has no built-in way to
            // project "the page" and "the total" out of a single
            // SELECT without materializing every row first, which would
            // defeat the whole point of paging.
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<IEnumerable<Item>> GetVariants(int parentItemId)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.BaseUnitOfMeasure)
                .Where(i => i.ParentItemId == parentItemId)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        // Stages only — see IUnitOfWork. CreateItemCommand stages this
        // together with the item's first ItemBarcode and commits both at
        // once, linked via navigation rather than a not-yet-assigned Id.
        public async Task<Item> AddAsync(Item item)
        {
            await _context.Items.AddAsync(item);
            return item;
        }

        public Task UpdateAsync(Item item)
        {
            _context.Items.Update(item);
            return Task.CompletedTask;
        }
    }
}

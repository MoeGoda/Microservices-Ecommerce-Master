using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly WarehouseContext _context;

        public PurchaseOrderRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<PurchaseOrder?> GetById(int id)
        {
            return await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Lines).ThenInclude(l => l.Item)
                .Include(p => p.Lines).ThenInclude(l => l.UnitOfMeasure)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<(IEnumerable<PurchaseOrder> Orders, int TotalCount)> GetPaged(int page, int pageSize)
        {
            var query = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Lines)
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (orders, totalCount);
        }

        public async Task<PurchaseOrder> AddAsync(PurchaseOrder purchaseOrder)
        {
            await _context.PurchaseOrders.AddAsync(purchaseOrder);
            return purchaseOrder;
        }

        public Task UpdateAsync(PurchaseOrder purchaseOrder)
        {
            _context.PurchaseOrders.Update(purchaseOrder);
            return Task.CompletedTask;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly WarehouseContext _context;

        public SupplierRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<Supplier?> GetById(int id)
        {
            return await _context.Suppliers.FindAsync(id);
        }

        public async Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> GetPaged(int page, int pageSize)
        {
            var query = _context.Suppliers.OrderBy(s => s.Name);

            var totalCount = await query.CountAsync();
            var suppliers = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (suppliers, totalCount);
        }

        public async Task<Supplier> AddAsync(Supplier supplier)
        {
            await _context.Suppliers.AddAsync(supplier);
            return supplier;
        }

        public Task UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            return Task.CompletedTask;
        }
    }
}

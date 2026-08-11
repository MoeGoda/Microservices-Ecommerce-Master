using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class UnitOfMeasureRepository : IUnitOfMeasureRepository
    {
        private readonly WarehouseContext _context;

        public UnitOfMeasureRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UnitOfMeasure>> GetAll()
        {
            return await _context.UnitsOfMeasure.OrderBy(u => u.Code).ToListAsync();
        }

        public async Task<UnitOfMeasure?> GetById(int id)
        {
            return await _context.UnitsOfMeasure.FindAsync(id);
        }

        public async Task<UnitOfMeasure?> GetByCode(string code)
        {
            return await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == code);
        }
    }
}

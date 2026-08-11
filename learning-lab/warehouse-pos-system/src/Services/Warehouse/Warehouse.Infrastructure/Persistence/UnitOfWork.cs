using Warehouse.Application.Contracts.Persistence;

namespace Warehouse.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly WarehouseContext _context;

        public UnitOfWork(WarehouseContext context)
        {
            _context = context;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

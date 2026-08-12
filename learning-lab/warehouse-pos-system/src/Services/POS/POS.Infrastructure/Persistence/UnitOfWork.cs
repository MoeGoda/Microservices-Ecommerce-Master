using POS.Application.Contracts.Persistence;

namespace POS.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PosContext _context;

        public UnitOfWork(PosContext context)
        {
            _context = context;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

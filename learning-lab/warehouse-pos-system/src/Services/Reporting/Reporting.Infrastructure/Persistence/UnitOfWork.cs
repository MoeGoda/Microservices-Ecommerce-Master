using Reporting.Application.Contracts.Persistence;

namespace Reporting.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ReportingContext _context;

        public UnitOfWork(ReportingContext context)
        {
            _context = context;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

using Notifications.Application.Contracts.Persistence;

namespace Notifications.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NotificationsContext _context;

        public UnitOfWork(NotificationsContext context)
        {
            _context = context;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

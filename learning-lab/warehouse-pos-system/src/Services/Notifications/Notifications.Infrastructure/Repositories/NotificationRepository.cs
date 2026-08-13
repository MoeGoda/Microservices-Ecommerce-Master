using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts.Persistence;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationsContext _context;

        public NotificationRepository(NotificationsContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            return notification;
        }

        public async Task<IEnumerable<Notification>> GetRecent(int take)
        {
            return await _context.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnread()
        {
            return await _context.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync();
        }

        public async Task<Notification?> GetById(int id)
        {
            return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<bool> ExistsForSale(int saleId)
        {
            return await _context.Notifications.AnyAsync(n => n.SourceSaleId == saleId);
        }

        public async Task<bool> ExistsForSaleReturn(int saleId)
        {
            return await _context.Notifications.AnyAsync(n => n.SourceSaleReturnId == saleId);
        }

        public Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            return Task.CompletedTask;
        }
    }
}

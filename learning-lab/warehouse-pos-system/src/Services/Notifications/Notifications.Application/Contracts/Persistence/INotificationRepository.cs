using Notifications.Domain.Entities;

namespace Notifications.Application.Contracts.Persistence
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<IEnumerable<Notification>> GetRecent(int take);
        Task<IEnumerable<Notification>> GetUnread();
        Task<Notification?> GetById(int id);
        Task<bool> ExistsForSale(int saleId);
        Task<bool> ExistsForSaleReturn(int saleId);
        Task UpdateAsync(Notification notification);
    }
}

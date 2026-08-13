using MediatR;
using Notifications.Application.Contracts.Persistence;

namespace Notifications.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead
{
    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, int>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkAllNotificationsAsReadCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var unread = (await _notificationRepository.GetUnread()).ToList();
            foreach (var notification in unread)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);
            }

            if (unread.Count > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return unread.Count;
        }
    }
}

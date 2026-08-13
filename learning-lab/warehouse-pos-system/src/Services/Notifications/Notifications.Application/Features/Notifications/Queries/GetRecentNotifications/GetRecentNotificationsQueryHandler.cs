using MediatR;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Models;

namespace Notifications.Application.Features.Notifications.Queries.GetRecentNotifications
{
    public class GetRecentNotificationsQueryHandler : IRequestHandler<GetRecentNotificationsQuery, IEnumerable<NotificationDto>>
    {
        private readonly INotificationRepository _notificationRepository;

        public GetRecentNotificationsQueryHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<IEnumerable<NotificationDto>> Handle(GetRecentNotificationsQuery request, CancellationToken cancellationToken)
        {
            var notifications = await _notificationRepository.GetRecent(request.Take);
            return notifications.Select(NotificationDto.FromEntity);
        }
    }
}

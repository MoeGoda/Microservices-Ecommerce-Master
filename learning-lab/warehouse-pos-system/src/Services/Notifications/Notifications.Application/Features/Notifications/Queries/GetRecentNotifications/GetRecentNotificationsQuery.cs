using MediatR;
using Notifications.Application.Models;

namespace Notifications.Application.Features.Notifications.Queries.GetRecentNotifications
{
    // What the bell dropdown loads once on open, before SignalR pushes take
    // over for anything that happens afterward.
    public class GetRecentNotificationsQuery : IRequest<IEnumerable<NotificationDto>>
    {
        public int Take { get; set; } = 20;
    }
}

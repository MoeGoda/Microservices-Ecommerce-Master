using Microsoft.AspNetCore.SignalR;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Models;

namespace Notifications.API.Realtime
{
    // The real implementation behind INotificationPusher (Application
    // layer). Broadcasts to EVERY connected client — there's no per-user
    // or per-role targeting yet (no concept of "who should see this"
    // exists anywhere in this system), the same simplicity level as
    // Reporting's dashboards showing every report to anyone with a valid
    // token. Splitting by role (e.g. only Managers get LowStock) is a
    // natural F-phase follow-up, not solved here.
    public class SignalRNotificationPusher : INotificationPusher
    {
        private const string ReceiveNotificationMethod = "ReceiveNotification";

        private readonly IHubContext<NotificationsHub> _hubContext;

        public SignalRNotificationPusher(IHubContext<NotificationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PushAsync(NotificationDto notification, CancellationToken cancellationToken)
        {
            return _hubContext.Clients.All.SendAsync(ReceiveNotificationMethod, notification, cancellationToken);
        }
    }
}

using Notifications.Application.Models;

namespace Notifications.Application.Contracts.Infrastructure
{
    // The Application layer's view of "tell whoever's listening right
    // now" — deliberately transport-agnostic, the same reason
    // IEventPublisher (POS/Warehouse) hides HTTP behind an interface. The
    // real implementation (SignalR, via IHubContext) lives in the API
    // layer, not Infrastructure — see Notifications.API's Realtime folder
    // for why: it's tied to this specific host's own request pipeline,
    // not a generic persistence/outbound-HTTP concern.
    public interface INotificationPusher
    {
        Task PushAsync(NotificationDto notification, CancellationToken cancellationToken);
    }
}

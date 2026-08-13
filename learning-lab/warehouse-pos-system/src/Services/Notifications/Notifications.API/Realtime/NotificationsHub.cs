using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notifications.API.Realtime
{
    // Deliberately empty — this app has no server-callable hub methods
    // (the client never calls INTO the hub, only listens). Its only job is
    // to exist as a typed target for IHubContext<NotificationsHub>, which
    // SignalRNotificationPusher uses to broadcast. [Authorize] here is
    // what actually gates the connection — see Program.cs's
    // OnMessageReceived for how the token gets from the query string (a
    // WebSocket handshake can't carry an Authorization header) into the
    // same JWT validation every other authenticated endpoint uses.
    [Authorize]
    public class NotificationsHub : Hub
    {
    }
}

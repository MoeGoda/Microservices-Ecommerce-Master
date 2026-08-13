namespace Notifications.Application.Contracts.Infrastructure
{
    // The Application layer's view of "tell whoever's on the alert list
    // by email" — deliberately transport-agnostic, the same
    // "Application knows WHAT to send, Infrastructure/API knows HOW"
    // split INotificationPusher already established for SignalR. No "to"
    // parameter, on purpose: like INotificationPusher broadcasting to
    // every connected client rather than a specific user (there's no
    // per-user targeting concept anywhere in this system yet — see
    // SignalRNotificationPusher's own comment), the audience for an email
    // alert is a fixed, deployment-level recipient list, not a decision
    // any individual command handler makes.
    public interface IEmailSender
    {
        Task SendAsync(string subject, string body, CancellationToken cancellationToken);
    }
}

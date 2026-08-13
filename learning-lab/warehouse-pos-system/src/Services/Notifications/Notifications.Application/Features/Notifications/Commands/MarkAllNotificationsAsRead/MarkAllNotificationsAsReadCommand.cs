using MediatR;

namespace Notifications.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead
{
    // Returns how many rows actually changed — 0 is a perfectly valid,
    // meaningful answer ("nothing was unread"), not an error.
    public class MarkAllNotificationsAsReadCommand : IRequest<int>
    {
    }
}

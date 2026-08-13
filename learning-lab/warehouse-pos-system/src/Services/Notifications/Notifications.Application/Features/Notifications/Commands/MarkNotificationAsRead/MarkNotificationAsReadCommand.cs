using MediatR;
using Notifications.Application.Models;

namespace Notifications.Application.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommand : IRequest<NotificationDto>
    {
        public int Id { get; set; }
    }
}

using Notifications.Domain.Entities;

namespace Notifications.Application.Models
{
    // What both the GET /Notifications response AND the live SignalR push
    // carry — the Angular client's "ReceiveNotification" handler and its
    // initial-load HTTP call render the exact same shape either way.
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public static NotificationDto FromEntity(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type.ToString(),
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
            };
        }
    }
}

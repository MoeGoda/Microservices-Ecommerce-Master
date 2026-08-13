// Mirrors Notifications.Application.Models.NotificationDto — the shape
// BOTH the GET /Notifications response and the live SignalR
// "ReceiveNotification" push carry (E1). type is 'SaleCompleted' |
// 'LowStock' today (Notifications.Domain.Entities.NotificationType's own
// members, serialized as strings).
export interface NotificationDto {
  id: number;
  type: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Services
{
    public interface INotificationService
    {
        Task<NotificationEntity> Send(NotificationEntity notification, string? recipientEmail = null);
        Task<List<NotificationEntity>> SendBulk(IEnumerable<NotificationEntity> notifications, IDictionary<int, string?>? recipientEmails = null);
        Task<List<NotificationEntity>> GetByRecipient(int recipientId);
        Task<List<NotificationEntity>> GetUnread(int recipientId);
        Task<int> GetUnreadCount(int recipientId);
        Task<NotificationEntity?> MarkAsRead(int notificationId);
        Task MarkAllRead(int recipientId);
        Task DeleteNotification(int notificationId);
        Task SendEmail(string recipientEmail, string subject, string body);
        Task<List<NotificationEntity>> GetAll();
    }
}
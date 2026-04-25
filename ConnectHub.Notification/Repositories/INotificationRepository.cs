using ConnectHub.Notification.Models;
using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Repositories
{
    public interface INotificationRepository
    {
        Task<NotificationEntity> AddAsync(NotificationEntity notification);
        Task<NotificationEntity?> FindByNotificationId(int notificationId);
        Task<List<NotificationEntity>> FindByRecipientId(int recipientId);
        Task<List<NotificationEntity>> FindUnreadByRecipientId(int recipientId);
        Task<int> CountUnreadByRecipientId(int recipientId);
        Task<List<NotificationEntity>> FindByRelatedId(int relatedId, string relatedType);
        Task MarkAllReadByRecipientId(int recipientId);
        Task DeleteByNotificationId(int notificationId);
        Task<List<NotificationEntity>> FindByType(NotificationType type);
        Task<List<NotificationEntity>> FindAll();
        Task SaveChanges();
    }
}
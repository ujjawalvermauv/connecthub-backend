using ConnectHub.Notification.Data;
using ConnectHub.Notification.Models;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _context;

        public NotificationRepository(NotificationDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationEntity> AddAsync(NotificationEntity notification)
        {
            var entry = await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<NotificationEntity?> FindByNotificationId(int notificationId)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(notification => notification.NotificationId == notificationId);
        }

        public async Task<List<NotificationEntity>> FindByRecipientId(int recipientId)
        {
            return await _context.Notifications
                .Where(notification => notification.RecipientId == recipientId)
                .OrderByDescending(notification => notification.SentAt)
                .ToListAsync();
        }

        public async Task<List<NotificationEntity>> FindUnreadByRecipientId(int recipientId)
        {
            return await _context.Notifications
                .Where(notification => notification.RecipientId == recipientId && !notification.IsRead)
                .OrderByDescending(notification => notification.SentAt)
                .ToListAsync();
        }

        public async Task<int> CountUnreadByRecipientId(int recipientId)
        {
            return await _context.Notifications
                .CountAsync(notification => notification.RecipientId == recipientId && !notification.IsRead);
        }

        public async Task<List<NotificationEntity>> FindByRelatedId(int relatedId, string relatedType)
        {
            return await _context.Notifications
                .Where(notification => notification.RelatedId == relatedId && notification.RelatedType == relatedType)
                .OrderByDescending(notification => notification.SentAt)
                .ToListAsync();
        }

        public async Task MarkAllReadByRecipientId(int recipientId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(notification => notification.RecipientId == recipientId && !notification.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteByNotificationId(int notificationId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(item => item.NotificationId == notificationId);
            if (notification == null)
            {
                return;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationEntity>> FindByType(NotificationType type)
        {
            return await _context.Notifications
                .Where(notification => notification.Type == type)
                .OrderByDescending(notification => notification.SentAt)
                .ToListAsync();
        }

        public async Task<List<NotificationEntity>> FindAll()
        {
            return await _context.Notifications
                .OrderByDescending(notification => notification.SentAt)
                .ToListAsync();
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}
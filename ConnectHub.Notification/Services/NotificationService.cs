using ConnectHub.Notification.Hubs;
using ConnectHub.Notification.Models;
using ConnectHub.Notification.Repositories;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IPresenceService _presenceService;
        private readonly SmtpSettings _smtpSettings;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHubContext<NotificationHub> hubContext,
            IPresenceService presenceService,
            IOptions<SmtpSettings> smtpSettings)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _presenceService = presenceService;
            _smtpSettings = smtpSettings.Value;
        }

        public async Task<NotificationEntity> Send(NotificationEntity notification, string? recipientEmail = null)
        {
            Validate(notification);

            notification.SentAt = DateTime.UtcNow;
            notification.IsRead = false;

            var savedNotification = await _notificationRepository.AddAsync(notification);
            await NotifyUnreadCountAsync(savedNotification.RecipientId);

            if (!_presenceService.IsUserOnline(savedNotification.RecipientId) && !string.IsNullOrWhiteSpace(recipientEmail))
            {
                await SendEmail(recipientEmail, savedNotification.Title, savedNotification.Message);
            }

            return savedNotification;
        }

        public async Task<List<NotificationEntity>> SendBulk(IEnumerable<NotificationEntity> notifications, IDictionary<int, string?>? recipientEmails = null)
        {
            var sentNotifications = new List<NotificationEntity>();

            foreach (var notification in notifications)
            {
                EnsurePlatformBroadcast(notification);

                string? recipientEmail = null;
                recipientEmails?.TryGetValue(notification.RecipientId, out recipientEmail);
                sentNotifications.Add(await Send(notification, recipientEmail));
            }

            return sentNotifications;
        }

        public Task<List<NotificationEntity>> GetByRecipient(int recipientId)
        {
            return _notificationRepository.FindByRecipientId(recipientId);
        }

        public Task<List<NotificationEntity>> GetUnread(int recipientId)
        {
            return _notificationRepository.FindUnreadByRecipientId(recipientId);
        }

        public Task<int> GetUnreadCount(int recipientId)
        {
            return _notificationRepository.CountUnreadByRecipientId(recipientId);
        }

        public async Task<NotificationEntity?> MarkAsRead(int notificationId)
        {
            var notification = await _notificationRepository.FindByNotificationId(notificationId);
            if (notification == null)
            {
                return null;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _notificationRepository.SaveChanges();
                await NotifyUnreadCountAsync(notification.RecipientId);
            }

            return notification;
        }

        public async Task MarkAllRead(int recipientId)
        {
            await _notificationRepository.MarkAllReadByRecipientId(recipientId);
            await NotifyUnreadCountAsync(recipientId);
        }

        public async Task DeleteNotification(int notificationId)
        {
            var notification = await _notificationRepository.FindByNotificationId(notificationId);
            if (notification == null)
            {
                return;
            }

            await _notificationRepository.DeleteByNotificationId(notificationId);
            await NotifyUnreadCountAsync(notification.RecipientId);
        }

        public async Task SendEmail(string recipientEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_smtpSettings.Host) || string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            {
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = body
            };

            using var client = new SmtpClient();
            var socketOptions = _smtpSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions);

            if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
            {
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public Task<List<NotificationEntity>> GetAll()
        {
            return _notificationRepository.FindAll();
        }

        private async Task NotifyUnreadCountAsync(int recipientId)
        {
            var unreadCount = await _notificationRepository.CountUnreadByRecipientId(recipientId);
            await _hubContext.Clients.User(recipientId.ToString()).SendAsync("NotificationCount", unreadCount);
        }

        private static void Validate(NotificationEntity notification)
        {
            if (notification.RecipientId <= 0)
            {
                throw new ArgumentException("RecipientId must be a valid user id.");
            }

            if (string.IsNullOrWhiteSpace(notification.Title))
            {
                throw new ArgumentException("Notification title is required.");
            }

            if (string.IsNullOrWhiteSpace(notification.Message))
            {
                throw new ArgumentException("Notification message is required.");
            }

            if (!Enum.IsDefined(typeof(NotificationType), notification.Type))
            {
                throw new ArgumentException("Notification type is invalid.");
            }

            if (notification.Type == NotificationType.PLATFORM)
            {
                return;
            }
        }

        private static void EnsurePlatformBroadcast(NotificationEntity notification)
        {
            if (notification.Type != NotificationType.PLATFORM)
            {
                throw new ArgumentException("Bulk notifications are restricted to PLATFORM broadcasts only.");
            }
        }
    }
}
using ConnectHub.Notification.Models;

namespace ConnectHub.Notification.DTOs
{
    public class SendNotificationRequest
    {
        public int RecipientId { get; set; }
        public int? SenderId { get; set; }
        public NotificationType Type { get; set; } = NotificationType.PLATFORM;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? RelatedId { get; set; }
        public string? RelatedType { get; set; }
        public string? RecipientEmail { get; set; }
    }

    public class BulkNotificationRecipientRequest
    {
        public int RecipientId { get; set; }
        public string? RecipientEmail { get; set; }
    }

    public class SendBulkNotificationRequest
    {
        public int? SenderId { get; set; }
        public NotificationType Type { get; set; } = NotificationType.PLATFORM;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? RelatedId { get; set; }
        public string? RelatedType { get; set; }
        public List<BulkNotificationRecipientRequest> Recipients { get; set; } = new();
    }
}
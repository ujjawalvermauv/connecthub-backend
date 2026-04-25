using ConnectHub.Notification.DTOs;
using ConnectHub.Notification.Models;
using ConnectHub.Notification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("byRecipient/{recipientId:int}")]
        public async Task<IActionResult> GetByRecipient([FromRoute] int recipientId)
        {
            var notifications = await _notificationService.GetByRecipient(recipientId);
            return Ok(notifications);
        }

        [HttpGet("unread/{recipientId:int}")]
        public async Task<IActionResult> GetUnread([FromRoute] int recipientId)
        {
            var notifications = await _notificationService.GetUnread(recipientId);
            return Ok(notifications);
        }

        [HttpGet("unreadCount/{recipientId:int}")]
        public async Task<IActionResult> GetUnreadCount([FromRoute] int recipientId)
        {
            var count = await _notificationService.GetUnreadCount(recipientId);
            return Ok(count);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await _notificationService.GetAll();
            return Ok(notifications);
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
        {
            try
            {
                var notification = await _notificationService.Send(new NotificationEntity
                {
                    RecipientId = request.RecipientId,
                    SenderId = request.SenderId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    RelatedId = request.RelatedId,
                    RelatedType = request.RelatedType
                }, request.RecipientEmail);

                return CreatedAtAction(nameof(GetByRecipient), new { recipientId = notification.RecipientId }, notification);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("sendBulk")]
        public async Task<IActionResult> SendBulk([FromBody] SendBulkNotificationRequest request)
        {
            try
            {
                if (request.Recipients.Count == 0)
                {
                    return BadRequest(new { message = "At least one recipient is required." });
                }

                if (request.Type != NotificationType.PLATFORM)
                {
                    return BadRequest(new { message = "Bulk notifications are restricted to PLATFORM broadcasts only." });
                }

                var notifications = request.Recipients.Select(recipient => new NotificationEntity
                {
                    RecipientId = recipient.RecipientId,
                    SenderId = request.SenderId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    RelatedId = request.RelatedId,
                    RelatedType = request.RelatedType
                }).ToList();

                var recipientEmails = request.Recipients
                    .Where(recipient => !string.IsNullOrWhiteSpace(recipient.RecipientEmail))
                    .ToDictionary(recipient => recipient.RecipientId, recipient => recipient.RecipientEmail);

                var result = await _notificationService.SendBulk(notifications, recipientEmails);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("markAsRead/{notificationId:int}")]
        public async Task<IActionResult> MarkAsRead([FromRoute] int notificationId)
        {
            var notification = await _notificationService.MarkAsRead(notificationId);
            if (notification == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPut("markAllRead/{recipientId:int}")]
        public async Task<IActionResult> MarkAllRead([FromRoute] int recipientId)
        {
            await _notificationService.MarkAllRead(recipientId);
            return NoContent();
        }

        [HttpDelete("{notificationId:int}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] int notificationId)
        {
            await _notificationService.DeleteNotification(notificationId);
            return NoContent();
        }
    }
}
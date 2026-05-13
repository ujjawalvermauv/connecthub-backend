using ConnectHub.Message.DTOs;
using ConnectHub.Message.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ConnectHub.Message.Data;

namespace ConnectHub.Message.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessageController> _logger;
        private readonly MessageDbContext _context;

        public MessageController(
    IMessageService messageService,
    ILogger<MessageController> logger,
    MessageDbContext context)
{
    _messageService = messageService;
    _logger = logger;
    _context = context;
}

        // -- Send (called by ChatHub after SignalR delivers the message) ---------

        // POST api/messages/direct
        [HttpPost("direct")]
        public async Task<IActionResult> SendDirectMessage([FromBody] CreateDirectMessageRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var message = await _messageService.SendMessage(new Models.Message
                {
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content.Trim(),
                    MessageType = request.MessageType,
                    MediaUrl = request.MediaUrl,
                    ReplyToMessageId = request.ReplyToMessageId,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    IsDeleted = false,
                    IsEdited = false
                });

                return CreatedAtAction(nameof(GetDirectMessages),
                    new { senderId = request.SenderId, receiverId = request.ReceiverId },
                    ToResponse(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending direct message");
                return StatusCode(500, "Failed to send message.");
            }
        }

        // POST api/messages/room
        [HttpPost("room")]
        public async Task<IActionResult> SendRoomMessage([FromBody] CreateRoomMessageRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var message = await _messageService.SendMessage(new Models.Message
                {
                    SenderId = request.SenderId,
                    RoomId = request.RoomId,
                    Content = request.Content.Trim(),
                    MessageType = request.MessageType,
                    MediaUrl = request.MediaUrl,
                    ReplyToMessageId = request.ReplyToMessageId,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    IsDeleted = false,
                    IsEdited = false
                });

                return CreatedAtAction(nameof(GetRoomMessages),
                    new { roomId = request.RoomId },
                    ToResponse(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending room message");
                return StatusCode(500, "Failed to send room message.");
            }
        }

        // -- Read ----------------------------------------------------------------

        // GET api/messages/direct?senderId=1&receiverId=2&page=1&pageSize=50
        [HttpGet("direct")]
        public async Task<IActionResult> GetDirectMessages(
            [FromQuery] int senderId,
            [FromQuery] int receiverId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var messages = await _messageService.GetDirectMessages(senderId, receiverId);
            var paged = messages
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToResponse)
                .ToList();

            return Ok(paged);
        }

        // GET api/messages/room/{roomId}?page=1&pageSize=50
        [HttpGet("room/{roomId:int}")]
        public async Task<IActionResult> GetRoomMessages(
            int roomId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var messages = await _messageService.GetRoomMessages(roomId);
            var paged = messages
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToResponse)
                .ToList();

            return Ok(paged);
        }

        // GET api/messages/unread/{userId}
        [HttpGet("unread/{userId:int}")]
        public async Task<IActionResult> GetUnreadMessages(int userId)
        {
            var messages = await _messageService.GetUnreadMessages(userId);
            return Ok(messages.Select(ToResponse));
        }

        // GET api/messages/unread-count/{userId}
        [HttpGet("unread-count/{userId:int}")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            var count = await _messageService.GetUnreadCount(userId);
            return Ok(new UnreadCountResponse { UserId = userId, UnreadCount = count });
        }

        // GET api/messages/recent/{userId}
        [HttpGet("recent/{userId}")]
        public async Task<IActionResult> GetRecentChats(int userId)
        {
            try
            {
                // ✅ STEP 1: Load all direct messages for this user into memory
                // Avoid EF Core GroupBy projection translation issues
                var messages = await _context.Messages
                    .Where(m => 
                        !m.IsDeleted &&
                        m.RoomId == null &&
                        (m.SenderId == userId || m.ReceiverId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();

                // ✅ STEP 2: Group in memory (not in SQL)
                var groupedChats = messages
                    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Where(g => g.Key.HasValue) // Only groups with valid partner IDs
                    .Select(g =>
                    {
                        var latestMessage = g.FirstOrDefault();
                        if (latestMessage == null) return null;

                        var unreadCount = g.Count(x =>
                            x.ReceiverId == userId &&
                            !x.IsRead);

                        return new
                        {
                            PartnerId = g.Key.Value,
                            LatestMessage = latestMessage,
                            UnreadCount = unreadCount
                        };
                    })
                    .Where(x => x != null)
                    .OrderByDescending(x => x.LatestMessage.SentAt)
                    .ToList();

                // ✅ STEP 3: Format response with field names frontend expects
                // Frontend's getOtherUserId() checks: user.userId, userId, senderId, receiverId, etc.
                var result = groupedChats
                    .Select(chat => new
                    {
                        userId = chat.PartnerId, // ✅ Named so frontend can find it with getOtherUserId()
                        lastMessage = new // ✅ Lowercase 'l' to match frontend's lastMessage check
                        {
                            content = chat.LatestMessage.Content,
                            createdAt = chat.LatestMessage.SentAt,
                            mediaUrl = chat.LatestMessage.MediaUrl,
                            messageType = chat.LatestMessage.MessageType
                        },
                        unreadCount = chat.UnreadCount,
                        sentAt = chat.LatestMessage.SentAt
                    })
                    .ToList();

                _logger.LogInformation("GetRecentChats: Retrieved {Count} recent chats for userId {UserId}", 
                    result.Count, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentChats for userId {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving recent chats" });
            }
        }
        // GET api/messages/search?senderId=1&receiverId=2&keyword=hello
        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages(
            [FromQuery] string keyword,
            [FromQuery] int? senderId,
            [FromQuery] int? receiverId,
            [FromQuery] int? roomId)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Keyword is required.");

            var messages = await _messageService.SearchMessages(keyword, senderId, receiverId, roomId);
            return Ok(messages.Select(ToResponse));
        }

        // GET api/messages/by-room/{roomId}
        [HttpGet("by-room/{roomId:int}")]
        public async Task<IActionResult> GetMessagesByRoom(int roomId)
        {
            var messages = await _messageService.GetMessagesByRoom(roomId);
            return Ok(messages.Select(ToResponse));
        }

        // -- Update --------------------------------------------------------------

        // PUT api/messages/{id}/read
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _messageService.MarkAsRead(id);
            return NoContent();
        }

        // PUT api/messages/read-all/{userId}
        [HttpPut("read-all/{userId:int}")]
        public async Task<IActionResult> MarkAllAsRead(int userId, [FromQuery] int? fromUserId)
        {
            await _messageService.MarkAllAsRead(userId, fromUserId);
            return NoContent();
        }

        // PUT api/messages/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] UpdateMessageRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updatedMessage = await _messageService.EditMessage(id, request.Content);
                if (updatedMessage == null) return NotFound();
                return Ok(ToResponse(updatedMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {Id}", id);
                return StatusCode(500, "Failed to edit message.");
            }
        }

        // -- Delete --------------------------------------------------------------

        // DELETE api/messages/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            await _messageService.DeleteMessage(id);
            return NoContent();
        }

        // -- Mapper --------------------------------------------------------------

        private static MessageResponse ToResponse(Models.Message m) => new()
        {
            MessageId = m.MessageId,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            RoomId = m.RoomId,
            // Hide content of soft-deleted messages
            Content = m.IsDeleted ? "[This message was deleted]" : m.Content,
            MessageType = m.MessageType,
            IsRead = m.IsRead,
            IsEdited = m.IsEdited,
            IsDeleted = m.IsDeleted,
            SentAt = m.SentAt,
            ReadAt = m.ReadAt,
            EditedAt = m.EditedAt,
            MediaUrl = m.MediaUrl,
            ReplyToMessageId = m.ReplyToMessageId
        };
    }
}

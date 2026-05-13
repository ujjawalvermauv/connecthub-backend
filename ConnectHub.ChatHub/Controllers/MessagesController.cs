using ConnectHub.ChatHub.Data;
using ConnectHub.ChatHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConnectHub.ChatHub.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly ChatHubDbContext _db;

        public MessagesController(ChatHubDbContext db)
        {
            _db = db;
        }

        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<RecentChatDto>>> GetRecentChats()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var currentUser = currentUserId.Value;

            var chats = await _db.Messages
                .Where(m => m.RoomId == null && (m.SenderId == currentUser || m.ReceiverId == currentUser))
                .Select(m => new
                {
                    Message = m,
                    PartnerId = m.SenderId == currentUser ? m.ReceiverId : m.SenderId
                })
                .Where(x => x.PartnerId != null)
                .GroupBy(x => x.PartnerId)
                .Select(g => new RecentChatDto
                {
                    User = new RecentUserDto
                    {
                        UserId = g.Key!.Value,
                        UserName = $"user{g.Key}",
                        DisplayName = $"User {g.Key}"
                    },
                    LastMessage = new RecentChatMessageDto
                    {
                        Content = g.OrderByDescending(x => x.Message.CreatedAt).First().Message.Content,
                        CreatedAt = g.OrderByDescending(x => x.Message.CreatedAt).First().Message.CreatedAt
                    },
                    UnreadCount = g.Count(x => x.Message.ReceiverId == currentUser && !x.Message.IsRead)
                })
                .OrderByDescending(x => x.LastMessage.CreatedAt)
                .ToListAsync();

            return Ok(chats);
        }

        [HttpGet("direct")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetDirectMessages([FromQuery] int senderId, [FromQuery] int receiverId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || (currentUserId.Value != senderId && currentUserId.Value != receiverId))
                return Unauthorized();

            var messages = await _db.Messages
                .Where(m => m.RoomId == null &&
                            ((m.SenderId == senderId && m.ReceiverId == receiverId) ||
                             (m.SenderId == receiverId && m.ReceiverId == senderId)))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    SentAt = m.CreatedAt,
                    IsRead = m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("direct")]
        public async Task<ActionResult<MessageDto>> SendDirectMessage([FromBody] CreateDirectMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value != request.SenderId)
                return Unauthorized();

            if (request.ReceiverId <= 0 || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { message = "receiverId and content are required." });

            var message = new Message
            {
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content.Trim(),
                MessageType = string.IsNullOrWhiteSpace(request.MessageType) ? "TEXT" : request.MessageType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new MessageDto
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                SentAt = message.CreatedAt,
                IsRead = message.IsRead
            });
        }

        [HttpPost("room")]
        public async Task<ActionResult<MessageDto>> SendRoomMessage([FromBody] CreateRoomMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value != request.SenderId)
                return Unauthorized();

            if (request.RoomId <= 0 || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { message = "roomId and content are required." });

            var message = new Message
            {
                SenderId = request.SenderId,
                RoomId = request.RoomId,
                Content = request.Content.Trim(),
                MessageType = string.IsNullOrWhiteSpace(request.MessageType) ? "TEXT" : request.MessageType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new MessageDto
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                RoomId = message.RoomId,
                Content = message.Content,
                SentAt = message.CreatedAt,
                IsRead = message.IsRead
            });
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] CreateMessageRequest request)
        {
            if (request.ReceiverId.HasValue)
                return await SendDirectMessage(new CreateDirectMessageRequest
                {
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId.Value,
                    Content = request.Content,
                    MessageType = request.MessageType
                });

            if (request.RoomId.HasValue)
                return await SendRoomMessage(new CreateRoomMessageRequest
                {
                    SenderId = request.SenderId,
                    RoomId = request.RoomId.Value,
                    Content = request.Content,
                    MessageType = request.MessageType
                });

            return BadRequest(new { message = "Either receiverId or roomId must be supplied." });
        }

        [HttpPut("{messageId}/read")]
        public async Task<ActionResult> MarkMessageRead(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var message = await _db.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (message == null)
                return NotFound();

            if (message.ReceiverId != currentUserId.Value)
                return Forbid();

            message.IsRead = true;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            return int.TryParse(userIdClaim, out var parsedId) ? parsedId : (int?)null;
        }

        public class CreateMessageRequest
        {
            public int SenderId { get; set; }
            public int? ReceiverId { get; set; }
            public int? RoomId { get; set; }
            public string Content { get; set; } = string.Empty;
            public string MessageType { get; set; } = "TEXT";
        }

        public class CreateDirectMessageRequest : CreateMessageRequest
        {
            public new int ReceiverId { get; set; }
        }

        public class CreateRoomMessageRequest : CreateMessageRequest
        {
            public new int RoomId { get; set; }
        }

        public class MessageDto
        {
            public int MessageId { get; set; }
            public int SenderId { get; set; }
            public int? ReceiverId { get; set; }
            public int? RoomId { get; set; }
            public string Content { get; set; } = string.Empty;
            public DateTime SentAt { get; set; }
            public bool IsRead { get; set; }
        }

        public class RecentChatDto
        {
            public RecentUserDto User { get; set; } = default!;
            public RecentChatMessageDto LastMessage { get; set; } = default!;
            public int UnreadCount { get; set; }
        }

        public class RecentUserDto
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }

        public class RecentChatMessageDto
        {
            public string Content { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}

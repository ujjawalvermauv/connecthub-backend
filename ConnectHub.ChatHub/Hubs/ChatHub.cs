using ConnectHub.ChatHub.Data;
using ConnectHub.ChatHub.Interfaces;
using ConnectHub.ChatHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.ChatHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IPresenceService _presence;
        private readonly ChatHubDbContext _db;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IPresenceService presence, ChatHubDbContext db, ILogger<ChatHub> logger)
        {
            _presence = presence;
            _db = db;
            _logger = logger;
        }

        private int GetUserId()
        {
            var value = Context.UserIdentifier;
            return int.TryParse(value, out var id) ? id : 0;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId == 0) { Context.Abort(); return; }

            var deviceInfo = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
            _presence.UserConnected(userId, Context.ConnectionId, deviceInfo);

            var roomIds = await _db.RoomMembers
                .Where(m => m.UserId == userId && m.IsActive)
                .Select(m => m.RoomId)
                .ToListAsync();

            foreach (var roomId in roomIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

            await Clients.Others.SendAsync("UserOnline", userId);
            _logger.LogInformation("User {UserId} connected.", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _presence.UserDisconnected(userId, Context.ConnectionId);

            if (!_presence.IsUserOnline(userId))
                await Clients.Others.SendAsync("UserOffline", userId);

            _logger.LogInformation("User {UserId} disconnected.", userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendDirectMessage(int receiverId, string content)
        {
            var senderId = GetUserId();
            if (senderId == 0 || string.IsNullOrWhiteSpace(content)) return;

            var payload = new { senderId, receiverId, content = content.Trim(), sentAt = DateTime.UtcNow, messageType = "TEXT" };
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", payload);
            await Clients.OthersInGroup($"user_{senderId}").SendAsync("ReceiveMessage", payload);
        }

        public async Task SendRoomMessage(int roomId, string content)
        {
            var senderId = GetUserId();
            if (senderId == 0 || string.IsNullOrWhiteSpace(content)) return;

            var isMember = await _db.RoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == senderId && m.IsActive);
            if (!isMember) { await Clients.Caller.SendAsync("Error", "You are not a member of this room."); return; }

            var payload = new { roomId, senderId, content = content.Trim(), sentAt = DateTime.UtcNow, messageType = "TEXT" };
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", payload);
        }

        public async Task JoinRoom(int roomId)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.RoomId == roomId && r.IsActive);
            if (room == null) { await Clients.Caller.SendAsync("Error", "Room not found."); return; }

            var currentCount = await _db.RoomMembers.CountAsync(m => m.RoomId == roomId && m.IsActive);
            if (currentCount >= room.MaxMembers) { await Clients.Caller.SendAsync("Error", "Room is full."); return; }

            var existing = await _db.RoomMembers.FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);
            if (existing == null)
                _db.RoomMembers.Add(new RoomMember { RoomId = roomId, UserId = userId, Role = "MEMBER", JoinedAt = DateTime.UtcNow, IsActive = true });
            else if (!existing.IsActive)
            { existing.IsActive = true; existing.JoinedAt = DateTime.UtcNow; }

            await _db.SaveChangesAsync();
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.Group(roomId.ToString()).SendAsync("UserJoinedRoom", new { roomId, userId });
        }

        public async Task LeaveRoom(int roomId)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            var membership = await _db.RoomMembers.FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);
            if (membership != null) { membership.IsActive = false; await _db.SaveChangesAsync(); }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.Group(roomId.ToString()).SendAsync("UserLeftRoom", new { roomId, userId });
        }

        public async Task TypingIndicator(int recipientId, bool isTyping)
        {
            var senderId = GetUserId();
            if (senderId == 0) return;
            await Clients.User(recipientId.ToString()).SendAsync("UserTyping", new { senderId, isTyping });
        }

        public async Task RoomTypingIndicator(int roomId, bool isTyping)
        {
            var senderId = GetUserId();
            if (senderId == 0) return;
            await Clients.OthersInGroup(roomId.ToString()).SendAsync("RoomUserTyping", new { roomId, senderId, isTyping });
        }

        public async Task MarkMessageRead(int messageId, int originalSenderId)
        {
            var readerId = GetUserId();
            if (readerId == 0) return;
            await Clients.User(originalSenderId.ToString()).SendAsync("MessageRead", new { messageId, readBy = readerId, readAt = DateTime.UtcNow });
        }

        public async Task SendMediaMessage(int receiverId, string mediaUrl, string messageType)
        {
            var senderId = GetUserId();
            if (senderId == 0 || string.IsNullOrWhiteSpace(mediaUrl)) return;
            var payload = new { senderId, receiverId, mediaUrl, messageType = messageType.ToUpper(), sentAt = DateTime.UtcNow };
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", payload);
        }

        public async Task SendRoomMediaMessage(int roomId, string mediaUrl, string messageType)
        {
            var senderId = GetUserId();
            if (senderId == 0 || string.IsNullOrWhiteSpace(mediaUrl)) return;
            var isMember = await _db.RoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == senderId && m.IsActive);
            if (!isMember) { await Clients.Caller.SendAsync("Error", "You are not a member of this room."); return; }
            var payload = new { roomId, senderId, mediaUrl, messageType = messageType.ToUpper(), sentAt = DateTime.UtcNow };
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", payload);
        }
    }
}

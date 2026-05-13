using ConnectHub.ChatHub.Data;
using ConnectHub.ChatHub.Interfaces;
using ConnectHub.ChatHub.Models;
using ConnectHub.ChatHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;

namespace ConnectHub.ChatHub.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat messaging.
    /// 
    /// Flowchart:
    /// Client: SendDirectMessage(receiverId, content)
    ///         ↓
    ///    ChatHub.SendDirectMessage()
    ///         ↓
    ///    Save to DB: POST http://localhost:5002/api/messages/direct
    ///         ↓
    ///    Broadcast: Clients.User(receiverId) → ReceiveMessage
    ///         ↓
    ///    Send Notification: POST http://localhost:5076/api/notifications/send
    /// 
    /// Requires: 
    /// - JWT Bearer token in query string: ?access_token=token
    /// - User must be authenticated (@Authorize)
    /// - Receiver must be online (has active connection)
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly IUserConnectionManager _connectionManager;
        private readonly IPresenceService _presence;
        private readonly ChatHubDbContext _db;
        private readonly ILogger<ChatHub> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        // Service URLs - MUST MATCH appsettings.json or actual service ports
        private readonly string _messageServiceUrl = "http://localhost:5002/api/messages";
        private readonly string _notificationServiceUrl = "http://localhost:5076/api/notifications";

        public ChatHub(
            IUserConnectionManager connectionManager,
            IPresenceService presence,
            ChatHubDbContext db,
            ILogger<ChatHub> logger,
            IHttpClientFactory httpClientFactory)
        {
            _connectionManager = connectionManager;
            _presence = presence;
            _db = db;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>Extract Bearer token from query string or Authorization header for service-to-service calls.</summary>
        private string? GetBearerToken()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return null;

            // Priority 1: Query string (from WebSocket connection)
            var accessToken = httpContext.Request.Query["access_token"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                return accessToken;
            }

            // Priority 2: Authorization header (for REST API calls from client)
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authVal))
            {
                var authHeader = authVal.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authHeader[7..].Trim();
                }
            }

            return null;
        }

        /// <summary>Extract userId from JWT claims (ClaimTypes.NameIdentifier).</summary>
        private int GetUserId()
        {
            var userIdString = Context.UserIdentifier;
            
            _logger.LogDebug("[GetUserId] Context.UserIdentifier: {UserIdentifier}", userIdString ?? "NULL");
            
            if (string.IsNullOrWhiteSpace(userIdString))
            {
                _logger.LogWarning("⚠ [GetUserId] Context.UserIdentifier is NULL or empty");
                
                // Debug: Check all claims
                if (Context.User?.Claims != null)
                {
                    _logger.LogDebug("[GetUserId] Available claims:");
                    foreach (var claim in Context.User.Claims)
                    {
                        _logger.LogDebug("  - {ClaimType}: {ClaimValue}", claim.Type, claim.Value);
                    }
                }
                return 0;
            }
            
            if (int.TryParse(userIdString, out var userId))
            {
                _logger.LogDebug("[GetUserId] Successfully parsed userId: {UserId}", userId);
                return userId;
            }

            _logger.LogWarning("⚠ [GetUserId] Failed to parse UserId from claims: {UserIdentifier}", userIdString);
            return 0;
        }

        /// <summary>Called when client connects to the hub.</summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var userId = GetUserId();
            var userIdentifier = Context.UserIdentifier;
            var httpContext = Context.GetHttpContext();
            var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("[Hub Connection] OnConnectedAsync called | ConnectionId: {ConnectionId} | UserId: {UserId} | UserIdentifier: {UserIdentifier} | ClientIp: {ClientIp}",
                connectionId, userId, userIdentifier ?? "NULL", clientIp ?? "UNKNOWN");

            if (userId == 0)
            {
                _logger.LogError("✗ [Hub Connection] Invalid user connection attempt (userId=0) | ConnectionId: {ConnectionId} | UserIdentifier: {UserIdentifier}",
                    connectionId, userIdentifier ?? "NULL");
                Context.Abort();
                return;
            }

            try
            {
                // Track connection
                var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
                _connectionManager.UserConnected(userId, connectionId, userAgent);
                _presence.UserConnected(userId, connectionId, userAgent);

                // Add user to room groups based on membership
                var roomIds = await _db.RoomMembers
                    .Where(m => m.UserId == userId && m.IsActive)
                    .Select(m => m.RoomId)
                    .ToListAsync();

                foreach (var roomId in roomIds)
                {
                    await Groups.AddToGroupAsync(connectionId, $"room-{roomId}");
                }

                // Broadcast user online status
                await Clients.Others.SendAsync("UserOnline", new { userId, connectionCount = _connectionManager.GetConnectionCountForUser(userId) });

                _logger.LogInformation(
                    "✓ [Hub Connection] User {UserId} connected successfully | ConnectionId: {ConnectionId} | ConnectionCount: {ConnectionCount} | RoomGroups: {RoomCount}",
                    userId, connectionId,
                    _connectionManager.GetConnectionCountForUser(userId),
                    roomIds.Count);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ [Hub Connection] Error in OnConnectedAsync for user {UserId} | ConnectionId: {ConnectionId}", userId, connectionId);
                Context.Abort();
            }
        }

        /// <summary>Called when client disconnects from the hub.</summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var connectionInfo = _connectionManager.GetConnectionInfo(connectionId);

            if (connectionInfo == null)
            {
                _logger.LogWarning("⚠ Unknown connection disconnected: {ConnectionId}", connectionId);
                await base.OnDisconnectedAsync(exception);
                return;
            }

            try
            {
                var userId = connectionInfo.UserId;

                // Remove connection tracking
                bool isCompletelyOffline = _connectionManager.UserDisconnected(connectionId);
                _presence.UserDisconnected(userId, connectionId);

                // If user is now completely offline, broadcast offline status
                if (isCompletelyOffline)
                {
                    await Clients.Others.SendAsync("UserOffline", new { userId });
                    _logger.LogInformation("✓ User {UserId} is now OFFLINE (all connections closed)", userId);
                }
                else
                {
                    _logger.LogInformation(
                        "✓ User {UserId} disconnected one connection (remaining: {Count}, still online)",
                        userId,
                        _connectionManager.GetConnectionCountForUser(userId));
                }

                if (exception != null)
                {
                    _logger.LogWarning(exception, "Connection disconnected with exception for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error in OnDisconnectedAsync");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Send direct message from sender to receiver.</summary>
        public async Task SendDirectMessage(int receiverId, string content)
        {
            var senderId = GetUserId();

            // Validation
            if (senderId == 0)
            {
                _logger.LogWarning("⚠ SendDirectMessage called with invalid senderId");
                await Clients.Caller.SendAsync("Error", "Unauthorized: Invalid user");
                return;
            }

            if (receiverId == 0)
            {
                await Clients.Caller.SendAsync("Error", "Invalid receiverId");
                return;
            }

            if (string.IsNullOrWhiteSpace(content) || content.Trim().Length == 0)
            {
                await Clients.Caller.SendAsync("Error", "Message cannot be empty");
                return;
            }

            if (senderId == receiverId)
            {
                await Clients.Caller.SendAsync("Error", "Cannot send message to yourself");
                return;
            }

            try
            {
                var cleanContent = content.Trim();

                // 1. Persist message via Message Service (port 5002)
                var client = _httpClientFactory.CreateClient();
                AttachAuthorizationHeader(client);

                var messageRequest = new
                {
                    senderId,
                    receiverId,
                    content = cleanContent,
                    messageType = "TEXT"
                };

                _logger.LogDebug("📤 Sending message from {SenderId} to {ReceiverId} (Message Service)", senderId, receiverId);

                var response = await client.PostAsJsonAsync($"{_messageServiceUrl}/direct", messageRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "✗ Message Service returned {StatusCode}: {Content}",
                        response.StatusCode,
                        await response.Content.ReadAsStringAsync());

                    await Clients.Caller.SendAsync("Error", "Failed to send message - Database error");
                    return;
                }

                var savedMessage = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var messageId = 0;
                if (savedMessage != null && savedMessage.RootElement.TryGetProperty("messageId", out var messageIdElement))
                {
                    messageId = messageIdElement.GetInt32();
                }

                _logger.LogInformation(
                    "✓ Message {MessageId} persisted from user {SenderId}",
                    messageId, senderId);

                // 2. Real-time broadcast via SignalR
                var messagePayload = new
                {
                    messageId,
                    senderId,
                    receiverId,
                    content = cleanContent,
                    sentAt = DateTime.UtcNow,
                    messageType = "TEXT",
                    isRead = false
                };

                // Debug: log outgoing realtime payload
                _logger.LogDebug("[Realtime Payload - Text] {Payload}", JsonSerializer.Serialize(messagePayload));

                // Send to receiver (if online)
                var receiverConnections = _connectionManager.GetConnectionsByUserId(receiverId).ToList();
                int receiverConnectionCount = receiverConnections.Count;

                if (receiverConnectionCount > 0)
                {
                    _logger.LogInformation("📡 Attempting realtime delivery to user {ReceiverId} via {Count} connections", receiverId, receiverConnectionCount);
                    await Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", messagePayload);
                    _logger.LogInformation("✓ Message delivered to {Count} active connections for user {ReceiverId}", receiverConnectionCount, receiverId);
                }
                else
                {
                    _logger.LogInformation("ℹ User {ReceiverId} is OFFLINE. Message queued in database.", receiverId);
                }

                // Send to sender (all connections for multi-device sync)
                var senderConnections = _connectionManager.GetConnectionsByUserId(senderId).ToList();
                if (senderConnections.Count > 0)
                {
                    await Clients.Clients(senderConnections).SendAsync("MessageSent", messagePayload);
                    _logger.LogInformation("✓ Sent acknowledgement to {Count} sender connections for user {SenderId}", senderConnections.Count, senderId);
                }
                else
                {
                    // Fallback to caller if something is wrong with tracking
                    await Clients.Caller.SendAsync("MessageSent", messagePayload);
                }

                // 3. Send notification (even if receiver is online, for badge/count updates)
                await SendDirectMessageNotificationAsync(senderId, receiverId, cleanContent, messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error sending direct message from {SenderId} to {ReceiverId}", senderId, receiverId);
                await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
            }
        }

        public async Task SendDirectMedia(int receiverId, string mediaUrl, string? caption = null)
        {
            var senderId = GetUserId();

            if (senderId == 0)
            {
                _logger.LogWarning("⚠ SendDirectMedia called with invalid senderId");
                await Clients.Caller.SendAsync("Error", "Unauthorized: Invalid user");
                return;
            }

            if (receiverId == 0)
            {
                await Clients.Caller.SendAsync("Error", "Invalid receiverId");
                return;
            }

            if (string.IsNullOrWhiteSpace(mediaUrl))
            {
                await Clients.Caller.SendAsync("Error", "MediaUrl is required");
                return;
            }

            if (senderId == receiverId)
            {
                await Clients.Caller.SendAsync("Error", "Cannot send message to yourself");
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                AttachAuthorizationHeader(client);

                var messageRequest = new
                {
                    senderId,
                    receiverId,
                    content = caption ?? string.Empty,
                    messageType = "IMAGE",
                    mediaUrl = mediaUrl
                };

                _logger.LogDebug("📤 Sending media message from {SenderId} to {ReceiverId} (Message Service)", senderId, receiverId);

                var response = await client.PostAsJsonAsync($"{_messageServiceUrl}/direct", messageRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "✗ Message Service returned {StatusCode}: {Content}",
                        response.StatusCode,
                        await response.Content.ReadAsStringAsync());

                    await Clients.Caller.SendAsync("Error", "Failed to send media message - Database error");
                    return;
                }

                var savedMessage = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var messageId = 0;
                if (savedMessage != null && savedMessage.RootElement.TryGetProperty("messageId", out var messageIdElement))
                {
                    messageId = messageIdElement.GetInt32();
                }

                _logger.LogInformation(
                    "✓ Media message {MessageId} persisted from user {SenderId}",
                    messageId, senderId);

                var messagePayload = new
                {
                    messageId,
                    senderId,
                    receiverId,
                    content = caption ?? string.Empty,
                    mediaUrl = mediaUrl,
                    sentAt = DateTime.UtcNow,
                    messageType = "IMAGE",
                    isRead = false
                };

                // Debug: log outgoing realtime media payload
                _logger.LogDebug("[Realtime Payload - Media] {Payload}", JsonSerializer.Serialize(messagePayload));

                var receiverConnections = _connectionManager.GetConnectionsByUserId(receiverId).ToList();
                int receiverConnectionCount = receiverConnections.Count;

                if (receiverConnectionCount > 0)
                {
                    _logger.LogInformation("📡 Attempting realtime delivery to user {ReceiverId} via {Count} connections", receiverId, receiverConnectionCount);
                    await Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", messagePayload);
                    _logger.LogInformation("✓ Media message delivered to {Count} active connections for user {ReceiverId}", receiverConnectionCount, receiverId);
                }
                else
                {
                    _logger.LogInformation("ℹ User {ReceiverId} is OFFLINE. Media message queued in database.", receiverId);
                }

                var senderConnections = _connectionManager.GetConnectionsByUserId(senderId).ToList();
                if (senderConnections.Count > 0)
                {
                    await Clients.Clients(senderConnections).SendAsync("MessageSent", messagePayload);
                    _logger.LogInformation("✓ Sent acknowledgement to {Count} sender connections for user {SenderId}", senderConnections.Count, senderId);
                }
                else
                {
                    await Clients.Caller.SendAsync("MessageSent", messagePayload);
                }

                await SendDirectMessageNotificationAsync(senderId, receiverId, caption ?? "[image]", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error sending direct media message from {SenderId} to {ReceiverId}", senderId, receiverId);
                await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
            }
        }

        /// <summary>Send notification to receiver about direct message.</summary>
        private async Task SendDirectMessageNotificationAsync(int senderId, int receiverId, string content, int messageId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var preview = content.Length > 50 ? content[..50] + "..." : content;

                var notificationRequest = new
                {
                    RecipientId = receiverId,
                    SenderId = senderId,
                    Type = "MESSAGE",
                    Title = "New message",
                    Message = preview,
                    RelatedId = messageId,
                    RelatedType = "DIRECT_MESSAGE"
                };

                _logger.LogDebug("📢 Sending notification to {ReceiverId} for message {MessageId}", receiverId, messageId);

                var response = await client.PostAsJsonAsync($"{_notificationServiceUrl}/send", notificationRequest);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✓ Notification sent for message {MessageId}", messageId);
                }
                else
                {
                    _logger.LogWarning(
                        "⚠ Notification Service returned {StatusCode} for messageId {MessageId}",
                        response.StatusCode, messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠ Failed to send notification for message {MessageId}", messageId);
                // Don't throw - notification failure shouldn't break message delivery
            }
        }

        /// <summary>Send message to a room.</summary>
        public async Task SendRoomMessage(int roomId, string content)
        {
            var senderId = GetUserId();

            if (senderId == 0)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                await Clients.Caller.SendAsync("Error", "Message cannot be empty");
                return;
            }

            try
            {
                // Verify membership
                var isMember = await _db.RoomMembers
                    .AnyAsync(m => m.RoomId == roomId && m.UserId == senderId && m.IsActive);

                if (!isMember)
                {
                    await Clients.Caller.SendAsync("Error", "You are not a member of this room");
                    return;
                }

                var cleanContent = content.Trim();

                // 1. Persist via Message Service
                var client = _httpClientFactory.CreateClient();
                AttachAuthorizationHeader(client);

                var messageRequest = new
                {
                    senderId,
                    roomId,
                    content = cleanContent,
                    messageType = "TEXT"
                };

                var response = await client.PostAsJsonAsync($"{_messageServiceUrl}/room", messageRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("✗ Failed to persist room message");
                    await Clients.Caller.SendAsync("Error", "Failed to send message");
                    return;
                }

                var savedMessage = await response.Content.ReadFromJsonAsync<dynamic>();

                // 2. Broadcast to room
                var messagePayload = new
                {
                    messageId = savedMessage?.messageId ?? 0,
                    roomId,
                    senderId,
                    content = cleanContent,
                    sentAt = DateTime.UtcNow,
                    messageType = "TEXT"
                };

                await Clients.Group($"room-{roomId}").SendAsync("ReceiveRoomMessage", messagePayload);

                _logger.LogInformation(
                    "✓ Room message from {SenderId} in {RoomId} broadcasted",
                    senderId, roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error sending room message");
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        /// <summary>User joins a room.</summary>
        public async Task JoinRoom(int roomId)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            try
            {
                var room = await _db.ChatRooms
                    .FirstOrDefaultAsync(r => r.RoomId == roomId && r.IsActive);

                if (room == null)
                {
                    await Clients.Caller.SendAsync("Error", "Room not found");
                    return;
                }

                var memberCount = await _db.RoomMembers
                    .CountAsync(m => m.RoomId == roomId && m.IsActive);

                if (memberCount >= room.MaxMembers)
                {
                    await Clients.Caller.SendAsync("Error", "Room is full");
                    return;
                }

                // Add/re-activate membership
                var existing = await _db.RoomMembers
                    .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);

                if (existing == null)
                {
                    _db.RoomMembers.Add(new RoomMember
                    {
                        RoomId = roomId,
                        UserId = userId,
                        Role = "MEMBER",
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                else if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.JoinedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                // Add to group and broadcast
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
                await Clients.Group($"room-{roomId}")
                    .SendAsync("UserJoinedRoom", new { roomId, userId, timestamp = DateTime.UtcNow });

                _logger.LogInformation("✓ User {UserId} joined room {RoomId}", userId, roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error joining room");
                await Clients.Caller.SendAsync("Error", "Failed to join room");
            }
        }

        /// <summary>User leaves a room.</summary>
        public async Task LeaveRoom(int roomId)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            try
            {
                var membership = await _db.RoomMembers
                    .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);

                if (membership != null)
                {
                    membership.IsActive = false;
                    await _db.SaveChangesAsync();
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
                await Clients.Group($"room-{roomId}")
                    .SendAsync("UserLeftRoom", new { roomId, userId, timestamp = DateTime.UtcNow });

                _logger.LogInformation("✓ User {UserId} left room {RoomId}", userId, roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error leaving room");
            }
        }

        /// <summary>Broadcast typing indicator to recipient.</summary>
        public async Task SendTypingIndicator(int recipientId, bool isTyping)
        {
            var senderId = GetUserId();
            if (senderId == 0) return;

            try
            {
                var receiverConnections = _connectionManager.GetConnectionsByUserId(recipientId).ToList();
                if (receiverConnections.Count > 0)
                {
                    await Clients.Clients(receiverConnections).SendAsync("TypingIndicator",
                        new { senderId, isTyping, timestamp = DateTime.UtcNow });
                }

                _logger.LogDebug("💬 Typing indicator: {SenderId} → {ReceiverId} ({IsTyping}) | Connections: {Count}",
                    senderId, recipientId, isTyping ? "typing" : "stopped", receiverConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "✗ Error sending typing indicator");
            }
        }

        /// <summary>Broadcast typing indicator to room.</summary>
        public async Task SendRoomTypingIndicator(int roomId, bool isTyping)
        {
            var senderId = GetUserId();
            if (senderId == 0) return;

            try
            {
                await Clients.OthersInGroup($"room-{roomId}").SendAsync("RoomTypingIndicator",
                    new { roomId, senderId, isTyping, timestamp = DateTime.UtcNow });

                _logger.LogDebug("💬 Room typing indicator: user {SenderId} in room {RoomId} ({IsTyping})",
                    senderId, roomId, isTyping ? "typing" : "stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "✗ Error sending room typing indicator");
            }
        }

        /// <summary>Attach Bearer token to HTTP client for service-to-service calls.</summary>
        private void AttachAuthorizationHeader(HttpClient client)
        {
            var token = GetBearerToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}

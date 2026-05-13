using ConnectHub.Notification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConnectHub.Notification.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notifications.
    /// Clients connect via: new HubConnectionBuilder()
    ///     .withUrl('http://localhost:5076/hubs/notifications?access_token=' + token)
    ///     .build()
    /// 
    /// Flow:
    /// Backend sends notification via NotificationService.Send()
    ///     ↓
    /// NotificationHub broadcasts via Clients.User(recipientId)
    ///     ↓
    /// Client receives "ReceiveNotification" event
    ///     ↓
    /// Unread badge/counter updated in real-time
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly IPresenceService _presenceService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(IPresenceService presenceService, ILogger<NotificationHub> logger)
        {
            _presenceService = presenceService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var userIdentifier = Context.UserIdentifier;
            var userId = GetCurrentUserId();
            var httpContext = Context.GetHttpContext();
            var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("[Hub Connection] NotificationHub.OnConnectedAsync called | ConnectionId: {ConnectionId} | UserId: {UserId} | UserIdentifier: {UserIdentifier} | ClientIp: {ClientIp}",
                connectionId, userId, userIdentifier ?? "NULL", clientIp ?? "UNKNOWN");

            if (userId == 0)
            {
                _logger.LogError("✗ [Hub Connection] NotificationHub connection failed: Unable to extract userId | ConnectionId: {ConnectionId} | UserIdentifier: {UserIdentifier}",
                    connectionId, userIdentifier ?? "NULL");
                Context.Abort();
                return;
            }

            try
            {
                var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
                _presenceService.UserConnected(userId, Context.ConnectionId, userAgent);

                // Broadcast user online status to all connected clients
                await Clients.All.SendAsync("UserOnline",
                    new
                    {
                        userId,
                        connectionCount = _presenceService.GetConnectionCount(userId),
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation(
                    "✓ [Hub Connection] NotificationHub: User {UserId} connected successfully | ConnectionId: {ConnectionId} | ConnectionCount: {ConnectionCount} | UserAgent: {UserAgent}",
                    userId, connectionId,
                    _presenceService.GetConnectionCount(userId),
                    userAgent ?? "Unknown");

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ [Hub Connection] Error in NotificationHub OnConnectedAsync for user {UserId} | ConnectionId: {ConnectionId}", userId, connectionId);
                Context.Abort();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            try
            {
                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    _logger.LogWarning("⚠ NotificationHub disconnect: Unable to extract userId for {ConnectionId}", connectionId);
                    await base.OnDisconnectedAsync(exception);
                    return;
                }

                _presenceService.UserDisconnected(userId, Context.ConnectionId);

                // Check if user is now completely offline
                if (!_presenceService.IsUserOnline(userId))
                {
                    await Clients.All.SendAsync("UserOffline",
                        new
                        {
                            userId,
                            timestamp = DateTime.UtcNow
                        });

                    _logger.LogInformation("✓ NotificationHub: User {UserId} is now OFFLINE (all connections closed)", userId);
                }
                else
                {
                    _logger.LogInformation(
                        "✓ NotificationHub: User {UserId} disconnected one connection (remaining: {Count}, still online)",
                        userId,
                        _presenceService.GetConnectionCount(userId));
                }

                if (exception != null)
                {
                    _logger.LogWarning(exception, "NotificationHub disconnection exception for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error in NotificationHub OnDisconnectedAsync");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Extract userId from JWT claims.
        /// Tries:
        /// 1. ClaimTypes.NameIdentifier (set by IUserIdProvider)
        /// 2. Fallback to query string parameter ?userId=<int>
        /// </summary>
        private int GetCurrentUserId()
        {
            // Priority 1: ClaimTypes.NameIdentifier from JWT
            var userIdentifier = Context.UserIdentifier;
            
            _logger.LogDebug("[GetCurrentUserId] Context.UserIdentifier: {UserIdentifier}", userIdentifier ?? "NULL");
            
            if (!string.IsNullOrWhiteSpace(userIdentifier))
            {
                if (int.TryParse(userIdentifier, out var userId))
                {
                    _logger.LogDebug("[GetCurrentUserId] Successfully parsed userId from NameIdentifier: {UserId}", userId);
                    return userId;
                }

                _logger.LogWarning(
                    "⚠ [GetCurrentUserId] Failed to parse userId from NameIdentifier claim: {UserIdentifier}",
                    userIdentifier);
                
                // Debug: Check all claims
                if (Context.User?.Claims != null)
                {
                    _logger.LogDebug("[GetCurrentUserId] Available claims in principal:");
                    foreach (var claim in Context.User.Claims)
                    {
                        _logger.LogDebug("  - {ClaimType}: {ClaimValue}", claim.Type, claim.Value);
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠ [GetCurrentUserId] Context.UserIdentifier is NULL or empty");
            }

            // Priority 2: Query string parameter (fallback)
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var queryUserId = httpContext.Request.Query["userId"].ToString();
                if (!string.IsNullOrWhiteSpace(queryUserId) && int.TryParse(queryUserId, out var userId))
                {
                    _logger.LogWarning(
                        "⚠ [GetCurrentUserId] Using userId from query string (should use JWT claims instead): {UserId}",
                        userId);
                    return userId;
                }
            }

            _logger.LogError(
                "✗ [GetCurrentUserId] Unable to resolve current user ID. Missing NameIdentifier claim or ?userId query parameter");

            return 0; // Return 0 instead of throwing to allow graceful abort in OnConnectedAsync
        }
    }
}

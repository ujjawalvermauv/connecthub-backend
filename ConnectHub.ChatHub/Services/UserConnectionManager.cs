using System.Collections.Concurrent;

namespace ConnectHub.ChatHub.Services
{
    /// <summary>
    /// Centralized connection tracking for SignalR hubs.
    /// Supports:
    /// - Multiple devices per user (same user can have multiple connections)
    /// - Connection metadata (device info, connection time, etc.)
    /// - Online/offline status tracking
    /// - Connection cleanup on disconnect
    /// 
    /// Thread-safe using ConcurrentDictionary and locks where needed.
    /// </summary>
    public class UserConnectionManager : IUserConnectionManager
    {
        private readonly ILogger<UserConnectionManager> _logger;

        /// <summary>Map: UserId → Set of ConnectionIds</summary>
        private readonly ConcurrentDictionary<int, HashSet<string>> _userConnections;

        /// <summary>Map: ConnectionId → User metadata</summary>
        private readonly ConcurrentDictionary<string, UserConnectionInfo> _connectionInfo;

        /// <summary>Lock object for thread-safe HashSet operations</summary>
        private readonly object _connectionLock = new object();

        public UserConnectionManager(ILogger<UserConnectionManager> logger)
        {
            _logger = logger;
            _userConnections = new ConcurrentDictionary<int, HashSet<string>>();
            _connectionInfo = new ConcurrentDictionary<string, UserConnectionInfo>();
        }

        /// <summary>
        /// Register a new user connection (called on SignalR connect).
        /// Supports multiple concurrent connections per user (multi-device/tab).
        /// </summary>
        public void UserConnected(int userId, string connectionId, string? userAgent = null)
        {
            if (userId <= 0) throw new ArgumentException("Invalid userId", nameof(userId));
            if (string.IsNullOrEmpty(connectionId)) throw new ArgumentNullException(nameof(connectionId));

            try
            {
                // Add/get user's connection set
                var connections = _userConnections.AddOrUpdate(
                    userId,
                    new HashSet<string> { connectionId },
                    (key, existing) =>
                    {
                        lock (_connectionLock)
                        {
                            existing.Add(connectionId);
                        }
                        return existing;
                    });

                // Store connection metadata
                var connectionInfo = new UserConnectionInfo
                {
                    UserId = userId,
                    ConnectionId = connectionId,
                    ConnectedAt = DateTime.UtcNow,
                    UserAgent = userAgent ?? "Unknown"
                };

                _connectionInfo[connectionId] = connectionInfo;

                _logger.LogInformation(
                    "✓ User {UserId} connected via {ConnectionId} (Total connections: {Count}, UserAgent: {UserAgent})",
                    userId, connectionId.Substring(0, 8) + "...", 
                    GetConnectionCountForUser(userId), 
                    userAgent ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error registering connection for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Unregister a user connection (called on SignalR disconnect).
        /// Returns true if user is now completely offline.
        /// </summary>
        public bool UserDisconnected(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return true;

            try
            {
                // Get connection info
                if (!_connectionInfo.TryRemove(connectionId, out var info))
                {
                    _logger.LogWarning("⚠ Unknown connection disconnected: {ConnectionId}", connectionId);
                    return true;
                }

                int userId = info.UserId;

                // Remove from user's connection set
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    lock (_connectionLock)
                    {
                        connections.Remove(connectionId);

                        // If user has no more connections, remove from online list
                        bool isCompletelyOffline = connections.Count == 0;
                        if (isCompletelyOffline)
                        {
                            _userConnections.TryRemove(userId, out _);
                        }

                        _logger.LogInformation(
                            "✓ User {UserId} disconnected via {ConnectionId} (Remaining connections: {Count}, Status: {Status})",
                            userId, 
                            connectionId.Substring(0, 8) + "...",
                            connections.Count,
                            isCompletelyOffline ? "OFFLINE" : "ONLINE");

                        return isCompletelyOffline;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error unregistering connection: {ConnectionId}", connectionId);
                return true;
            }
        }

        /// <summary>Get all connection IDs for a user.</summary>
        public IEnumerable<string> GetConnectionsByUserId(int userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (_connectionLock)
                {
                    return connections.ToList();
                }
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>Get the number of active connections for a user.</summary>
        public int GetConnectionCountForUser(int userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (_connectionLock)
                {
                    return connections.Count;
                }
            }
            return 0;
        }

        /// <summary>Check if a user is currently online (has at least one connection).</summary>
        public bool IsUserOnline(int userId)
        {
            return GetConnectionCountForUser(userId) > 0;
        }

        /// <summary>Get all currently online user IDs.</summary>
        public IEnumerable<int> GetOnlineUserIds()
        {
            return _userConnections.Keys.OrderBy(k => k);
        }

        /// <summary>Get connection info for a specific connection ID.</summary>
        public UserConnectionInfo? GetConnectionInfo(string connectionId)
        {
            _connectionInfo.TryGetValue(connectionId, out var info);
            return info;
        }

        /// <summary>Get connection info for all of a user's connections.</summary>
        public List<UserConnectionInfo> GetUserConnectionsInfo(int userId)
        {
            var connections = GetConnectionsByUserId(userId);
            var result = new List<UserConnectionInfo>();

            foreach (var connId in connections)
            {
                if (_connectionInfo.TryGetValue(connId, out var info))
                {
                    result.Add(info);
                }
            }

            return result;
        }

        /// <summary>Get total number of unique online users.</summary>
        public int GetTotalOnlineUsers()
        {
            return _userConnections.Count;
        }

        /// <summary>Get total number of all connections (handles multi-device).</summary>
        public int GetTotalConnections()
        {
            int total = 0;
            foreach (var connections in _userConnections.Values)
            {
                lock (_connectionLock)
                {
                    total += connections.Count;
                }
            }
            return total;
        }

        /// <summary>Clear all connections for a user (forced logout).</summary>
        public void ClearUserConnections(int userId)
        {
            if (_userConnections.TryRemove(userId, out var connections))
            {
                lock (_connectionLock)
                {
                    foreach (var connId in connections)
                    {
                        _connectionInfo.TryRemove(connId, out _);
                    }
                }

                _logger.LogInformation("🔄 Cleared all connections for user {UserId}", userId);
            }
        }

        /// <summary>Get detailed online users info for dashboard/admin purposes.</summary>
        public Dictionary<int, OnlineUserInfo> GetOnlineUsersInfo()
        {
            var result = new Dictionary<int, OnlineUserInfo>();

            foreach (var kvp in _userConnections)
            {
                int userId = kvp.Key;
                var connections = kvp.Value;

                lock (_connectionLock)
                {
                    var userConnInfo = new OnlineUserInfo
                    {
                        UserId = userId,
                        TotalConnections = connections.Count,
                        Connections = connections
                            .Select(connId => _connectionInfo.TryGetValue(connId, out var info) ? info : null)
                            .Where(info => info != null)
                            .Cast<UserConnectionInfo>()
                            .ToList()
                    };

                    result[userId] = userConnInfo;
                }
            }

            return result;
        }
    }

    /// <summary>Metadata about a single connection.</summary>
    public class UserConnectionInfo
    {
        public int UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public string UserAgent { get; set; } = string.Empty;

        public TimeSpan ConnectionDuration => DateTime.UtcNow - ConnectedAt;
    }

    /// <summary>Aggregated online user info.</summary>
    public class OnlineUserInfo
    {
        public int UserId { get; set; }
        public int TotalConnections { get; set; }
        public List<UserConnectionInfo> Connections { get; set; } = new();
    }

    /// <summary>Interface for dependency injection.</summary>
    public interface IUserConnectionManager
    {
        void UserConnected(int userId, string connectionId, string? userAgent = null);
        bool UserDisconnected(string connectionId);
        IEnumerable<string> GetConnectionsByUserId(int userId);
        int GetConnectionCountForUser(int userId);
        bool IsUserOnline(int userId);
        IEnumerable<int> GetOnlineUserIds();
        UserConnectionInfo? GetConnectionInfo(string connectionId);
        List<UserConnectionInfo> GetUserConnectionsInfo(int userId);
        int GetTotalOnlineUsers();
        int GetTotalConnections();
        void ClearUserConnections(int userId);
        Dictionary<int, OnlineUserInfo> GetOnlineUsersInfo();
    }
}

using System.Collections.Concurrent;
using ConnectHub.ChatHub.Models;

namespace ConnectHub.ChatHub.Services
{
    public class PresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();
        private readonly ConcurrentDictionary<string, UserConnection> _connectionInfo = new();

        public void UserConnected(int userId, string connectionId, string? deviceInfo = null)
        {
            var connectionSet = _connections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connectionSet)
            {
                connectionSet.Add(connectionId);
            }

            _connectionInfo[connectionId] = new UserConnection
            {
                ConnectionId = connectionId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow,
                DeviceInfo = deviceInfo
            };
        }

        public void UserDisconnected(int userId, string connectionId)
        {
            if (_connections.TryGetValue(userId, out var connectionSet))
            {
                lock (connectionSet)
                {
                    connectionSet.Remove(connectionId);
                    if (connectionSet.Count == 0)
                    {
                        _connections.TryRemove(userId, out _);
                    }
                }
            }

            _connectionInfo.TryRemove(connectionId, out _);
        }

        public List<string> GetConnectionsByUserId(int userId)
        {
            if (!_connections.TryGetValue(userId, out var connectionSet))
            {
                return [];
            }

            lock (connectionSet)
            {
                return connectionSet.ToList();
            }
        }

        public List<int> GetOnlineUserIds()
        {
            return _connections.Keys.ToList();
        }

        public bool IsUserOnline(int userId)
        {
            return _connections.ContainsKey(userId);
        }

        public int GetConnectionCount(int userId)
        {
            if (!_connections.TryGetValue(userId, out var connectionSet))
            {
                return 0;
            }

            lock (connectionSet)
            {
                return connectionSet.Count;
            }
        }

        public List<UserConnection> GetOnlineUsersInfo()
        {
            return _connectionInfo.Values
                .GroupBy(connection => connection.UserId)
                .Select(group => group.First())
                .OrderBy(connection => connection.UserId)
                .ToList();
        }

        public void ClearUserConnections(int? userId = null)
        {
            if (userId.HasValue)
            {
                if (_connections.TryRemove(userId.Value, out var connectionSet))
                {
                    lock (connectionSet)
                    {
                        foreach (var connectionId in connectionSet)
                        {
                            _connectionInfo.TryRemove(connectionId, out _);
                        }
                    }
                }

                return;
            }

            _connections.Clear();
            _connectionInfo.Clear();
        }
    }
}

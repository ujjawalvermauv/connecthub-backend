using System.Collections.Concurrent;
using ConnectHub.ChatHub.Interfaces;
using ConnectHub.ChatHub.Models;

namespace ConnectHub.ChatHub.Services
{
    public class PresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();
        private readonly ConcurrentDictionary<string, UserConnection> _userInfo = new();

        public void UserConnected(int userId, string connectionId, string? deviceInfo = null)
        {
            var connections = _connections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connections) { connections.Add(connectionId); }
            _userInfo[connectionId] = new UserConnection
            {
                ConnectionId = connectionId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow,
                DeviceInfo = deviceInfo
            };
        }

        public void UserDisconnected(int userId, string connectionId)
        {
            if (_connections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                        _connections.TryRemove(userId, out _);
                }
            }
            _userInfo.TryRemove(connectionId, out _);
        }

        public List<string> GetConnectionsByUserId(int userId)
        {
            if (_connections.TryGetValue(userId, out var connections))
                lock (connections) { return connections.ToList(); }
            return new List<string>();
        }

        public List<int> GetOnlineUserIds() => _connections.Keys.ToList();

        public bool IsUserOnline(int userId)
            => _connections.TryGetValue(userId, out var c) && c.Count > 0;

        public int GetConnectionCount() => _userInfo.Count;

        public List<UserConnection> GetOnlineUsersInfo() => _userInfo.Values.ToList();

        public void ClearUserConnections(int userId)
        {
            if (_connections.TryRemove(userId, out var connections))
                lock (connections)
                    foreach (var connId in connections)
                        _userInfo.TryRemove(connId, out _);
        }
    }
}

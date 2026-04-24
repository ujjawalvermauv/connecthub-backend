using ConnectHub.Notification.Models;

namespace ConnectHub.Notification.Services
{
    public interface IPresenceService
    {
        void UserConnected(int userId, string connectionId, string? deviceInfo = null);
        void UserDisconnected(int userId, string connectionId);
        bool IsUserOnline(int userId);
        int GetConnectionCount(int userId);
        List<int> GetOnlineUserIds();
        List<string> GetConnectionsByUserId(int userId);
        List<UserConnection> GetOnlineUsersInfo();
        void ClearUserConnections(int? userId = null);
    }
}
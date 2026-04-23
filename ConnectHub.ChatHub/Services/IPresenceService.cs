using ConnectHub.ChatHub.Models;

namespace ConnectHub.ChatHub.Services
{
    public interface IPresenceService
    {
        void UserConnected(int userId, string connectionId, string? deviceInfo = null);
        void UserDisconnected(int userId, string connectionId);
        List<string> GetConnectionsByUserId(int userId);
        List<int> GetOnlineUserIds();
        bool IsUserOnline(int userId);
        int GetConnectionCount(int userId);
        List<UserConnection> GetOnlineUsersInfo();
        void ClearUserConnections(int? userId = null);
    }
}

using ConnectHub.ChatHub.Models;

namespace ConnectHub.ChatHub.Services
{
    public interface IChatRoomService
    {
        Task<ChatRoom> CreateRoom(ChatRoom room);
        Task<ChatRoom?> GetRoomById(int roomId);
        Task<List<ChatRoom>> GetRoomsByUser(int userId);
        Task<List<ChatRoom>> GetPublicRooms();
        Task<ChatRoom?> UpdateRoom(int roomId, ChatRoom room);
        Task DeleteRoom(int roomId);
        Task AddMember(int roomId, int userId, string role = "MEMBER");
        Task RemoveMember(int roomId, int userId);
        Task<List<RoomMember>> GetMembers(int roomId);
        Task UpdateMemberRole(int roomId, int userId, string role);
        Task LeaveRoom(int roomId, int userId);
        Task<bool> IsUserInRoom(int roomId, int userId);
        Task<int> GetMemberCount(int roomId);
    }
}

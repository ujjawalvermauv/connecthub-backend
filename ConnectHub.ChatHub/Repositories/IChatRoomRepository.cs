using ConnectHub.ChatHub.Models;

namespace ConnectHub.ChatHub.Repositories
{
    public interface IChatRoomRepository
    {
        Task<ChatRoom?> FindByRoomId(int roomId);
        Task<List<ChatRoom>> FindByCreatedBy(int createdBy);
        Task<ChatRoom?> FindByRoomName(string roomName);
        Task<List<ChatRoom>> FindRoomsByUserId(int userId);
        Task<List<RoomMember>> FindMembersByRoomId(int roomId);
        Task<bool> IsUserInRoom(int roomId, int userId);
        Task<int> CountMembersByRoomId(int roomId);
        Task<List<ChatRoom>> FindPublicRooms();
        Task<ChatRoom> AddRoom(ChatRoom room);
        Task<RoomMember> AddMember(RoomMember member);
        Task SaveChanges();
    }
}

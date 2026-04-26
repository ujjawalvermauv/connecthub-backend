using System.Collections.Generic;

namespace ConnectHub.Web.Services
{
    public interface IChatRoomService
    {
        IEnumerable<string> GetAllRooms();
        void CreateRoom(string name);
        void JoinRoom(int roomId, int userId);
        void LeaveRoom(int roomId, int userId);
    }

    public class ChatRoomService : IChatRoomService
    {
        public IEnumerable<string> GetAllRooms()
        {
            // TODO: Implement DB logic
            return new List<string> { "General", "Random" };
        }
        public void CreateRoom(string name)
        {
            // TODO: Implement create logic
        }
        public void JoinRoom(int roomId, int userId)
        {
            // TODO: Implement join logic
        }
        public void LeaveRoom(int roomId, int userId)
        {
            // TODO: Implement leave logic
        }
    }
}

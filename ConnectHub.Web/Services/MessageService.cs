using System.Collections.Generic;

namespace ConnectHub.Web.Services
{
    public interface IMessageService
    {
        IEnumerable<string> GetMessagesForRoom(int roomId);
        void SendMessage(int roomId, string user, string message);
    }

    public class MessageService : IMessageService
    {
        public IEnumerable<string> GetMessagesForRoom(int roomId)
        {
            // TODO: Implement DB logic
            return new List<string> { "Welcome to room " + roomId };
        }
        public void SendMessage(int roomId, string user, string message)
        {
            // TODO: Implement message save logic
        }
    }
}

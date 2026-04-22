using ConnectHub.Message.Models;
using MessageEntity = ConnectHub.Message.Models.Message;

namespace ConnectHub.Message.Services
{
    public interface IMessageService
    {
        Task<MessageEntity> SendMessage(MessageEntity message);
        Task<MessageEntity?> GetMessageById(int id);
        Task<List<MessageEntity>> GetDirectMessages(int senderId, int receiverId);
        Task<List<MessageEntity>> GetRoomMessages(int roomId);
        Task<List<MessageEntity>> GetUnreadMessages(int receiverId);
        Task MarkAsRead(int messageId);
        Task MarkAllAsRead(int receiverId, int? roomId = null);
        Task<MessageEntity?> EditMessage(int messageId, string content);
        Task DeleteMessage(int messageId);
        Task<int> GetUnreadCount(int receiverId);
        Task<List<MessageEntity>> GetRecentChats(int userId);
        Task<List<MessageEntity>> SearchMessages(string query, int userId);
        Task<List<MessageEntity>> GetMessagesByRoom(int roomId);
    }
}

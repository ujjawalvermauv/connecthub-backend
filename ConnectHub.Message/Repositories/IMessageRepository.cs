using ConnectHub.Message.Models;
using MessageEntity = ConnectHub.Message.Models.Message;

namespace ConnectHub.Message.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageEntity?> FindByMessageId(int messageId);
        Task<List<MessageEntity>> FindBySenderAndReceiver(int senderId, int receiverId);
        Task<List<MessageEntity>> FindByRoomId(int roomId);
        Task<List<MessageEntity>> FindUnreadByReceiverId(int receiverId);
        Task<List<MessageEntity>> FindRecentMessages(int userId);
        Task<int> CountUnreadByReceiverId(int receiverId);
        Task MarkAllReadByRoomId(int roomId);
        Task DeleteByMessageId(int messageId);
        Task<List<MessageEntity>> SearchMessages(string query, int userId);
        Task<MessageEntity> AddMessage(MessageEntity message);
        Task SaveChanges();
    }
}

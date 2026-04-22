using ConnectHub.Message.Models;
using ConnectHub.Message.Repositories;
using MessageEntity = ConnectHub.Message.Models.Message;

namespace ConnectHub.Message.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<MessageEntity> SendMessage(MessageEntity message)
        {
            if (message.SenderId <= 0)
            {
                throw new ArgumentException("SenderId must be a valid user id.");
            }

            var hasReceiver = message.ReceiverId.HasValue;
            var hasRoom = message.RoomId.HasValue;

            if (hasReceiver == hasRoom)
            {
                throw new ArgumentException("Exactly one of ReceiverId or RoomId must be provided.");
            }

            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(message.MediaUrl))
            {
                throw new ArgumentException("Message must contain text content or media.");
            }

            message.SentAt = DateTime.UtcNow;
            message.IsDeleted = false;
            message.IsEdited = false;
            message.IsRead = false;

            return await _messageRepository.AddMessage(message);
        }

        public Task<MessageEntity?> GetMessageById(int id)
        {
            return _messageRepository.FindByMessageId(id);
        }

        public Task<List<MessageEntity>> GetDirectMessages(int senderId, int receiverId)
        {
            return _messageRepository.FindBySenderAndReceiver(senderId, receiverId);
        }

        public Task<List<MessageEntity>> GetRoomMessages(int roomId)
        {
            return _messageRepository.FindByRoomId(roomId);
        }

        public Task<List<MessageEntity>> GetUnreadMessages(int receiverId)
        {
            return _messageRepository.FindUnreadByReceiverId(receiverId);
        }

        public async Task MarkAsRead(int messageId)
        {
            var message = await _messageRepository.FindByMessageId(messageId);
            if (message == null)
            {
                return;
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _messageRepository.SaveChanges();
            }
        }

        public async Task MarkAllAsRead(int receiverId, int? roomId = null)
        {
            if (roomId.HasValue)
            {
                await _messageRepository.MarkAllReadByRoomId(roomId.Value);
                return;
            }

            var unread = await _messageRepository.FindUnreadByReceiverId(receiverId);
            var readAt = DateTime.UtcNow;

            foreach (var message in unread)
            {
                message.IsRead = true;
                message.ReadAt = readAt;
            }

            await _messageRepository.SaveChanges();
        }

        public async Task<MessageEntity?> EditMessage(int messageId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Edited content cannot be empty.");
            }

            var message = await _messageRepository.FindByMessageId(messageId);
            if (message == null)
            {
                return null;
            }

            message.Content = content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            await _messageRepository.SaveChanges();

            return message;
        }

        public Task DeleteMessage(int messageId)
        {
            return _messageRepository.DeleteByMessageId(messageId);
        }

        public Task<int> GetUnreadCount(int receiverId)
        {
            return _messageRepository.CountUnreadByReceiverId(receiverId);
        }

        public Task<List<MessageEntity>> GetRecentChats(int userId)
        {
            return _messageRepository.FindRecentMessages(userId);
        }

        public Task<List<MessageEntity>> SearchMessages(string query, int userId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(new List<MessageEntity>());
            }

            return _messageRepository.SearchMessages(query, userId);
        }

        public Task<List<MessageEntity>> GetMessagesByRoom(int roomId)
        {
            return _messageRepository.FindByRoomId(roomId);
        }
    }
}

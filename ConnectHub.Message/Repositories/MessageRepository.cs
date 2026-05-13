using ConnectHub.Message.Data;
using ConnectHub.Message.Models;
using Microsoft.EntityFrameworkCore;
using MessageEntity = ConnectHub.Message.Models.Message;

namespace ConnectHub.Message.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly MessageDbContext _context;

        public MessageRepository(MessageDbContext context)
        {
            _context = context;
        }

        public async Task<MessageEntity> AddMessage(MessageEntity message)
        {
            var entry = await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<MessageEntity?> FindByMessageId(int messageId)
        {
            return await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);
        }

        public async Task<List<MessageEntity>> FindBySenderAndReceiver(int senderId, int receiverId)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted && m.RoomId == null &&
                            ((m.SenderId == senderId && m.ReceiverId == receiverId) ||
                             (m.SenderId == receiverId && m.ReceiverId == senderId)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<MessageEntity>> FindByRoomId(int roomId)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted && m.RoomId == roomId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<MessageEntity>> FindUnreadByReceiverId(int receiverId)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted && m.ReceiverId == receiverId && !m.IsRead)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<MessageEntity>> FindRecentMessages(int userId)
        {
            var directMessages = await _context.Messages
                .Where(m => !m.IsDeleted && m.RoomId == null && m.ReceiverId != null &&
                            (m.SenderId == userId || m.ReceiverId == userId))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return directMessages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId!.Value : m.SenderId)
                .Select(group => group.First())
                .OrderByDescending(m => m.SentAt)
                .ToList();
        }

        public async Task<int> CountUnreadByReceiverId(int receiverId)
        {
            return await _context.Messages
                .CountAsync(m => !m.IsDeleted && m.ReceiverId == receiverId && !m.IsRead);
        }

        public async Task MarkAllReadByRoomId(int roomId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => !m.IsDeleted && m.RoomId == roomId && !m.IsRead)
                .ToListAsync();

            var readAt = DateTime.UtcNow;
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = readAt;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteByMessageId(int messageId)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (message == null)
            {
                return;
            }

            message.IsDeleted = true;
            message.Content = "[deleted]";
            await _context.SaveChangesAsync();
        }

        public async Task<List<MessageEntity>> SearchMessages(string keyword, int? senderId, int? receiverId, int? roomId)
        {
            keyword = keyword.Trim();

            var query = _context.Messages
                .Where(m => !m.IsDeleted && EF.Functions.Like(m.Content, $"%{keyword}%"));

            if (roomId.HasValue)
            {
                query = query.Where(m => m.RoomId == roomId.Value);
            }

            if (senderId.HasValue)
            {
                query = query.Where(m => m.SenderId == senderId.Value);
            }

            if (receiverId.HasValue)
            {
                query = query.Where(m => m.ReceiverId == receiverId.Value);
            }

            return await query
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}

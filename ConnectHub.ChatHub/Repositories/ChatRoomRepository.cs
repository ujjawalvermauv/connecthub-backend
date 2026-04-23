using ConnectHub.ChatHub.Data;
using ConnectHub.ChatHub.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.ChatHub.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly ChatRoomDbContext _context;

        public ChatRoomRepository(ChatRoomDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom?> FindByRoomId(int roomId)
        {
            return await _context.ChatRooms.FirstOrDefaultAsync(room => room.RoomId == roomId && room.IsActive);
        }

        public async Task<List<ChatRoom>> FindByCreatedBy(int createdBy)
        {
            return await _context.ChatRooms
                .Where(room => room.CreatedBy == createdBy && room.IsActive)
                .OrderByDescending(room => room.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatRoom?> FindByRoomName(string roomName)
        {
            return await _context.ChatRooms.FirstOrDefaultAsync(room => room.RoomName == roomName && room.IsActive);
        }

        public async Task<List<ChatRoom>> FindRoomsByUserId(int userId)
        {
            var roomIds = _context.RoomMembers
                .Where(member => member.UserId == userId && member.IsActive)
                .Select(member => member.RoomId);

            return await _context.ChatRooms
                .Where(room => room.IsActive && roomIds.Contains(room.RoomId))
                .OrderByDescending(room => room.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RoomMember>> FindMembersByRoomId(int roomId)
        {
            return await _context.RoomMembers
                .Where(member => member.RoomId == roomId && member.IsActive)
                .OrderBy(member => member.JoinedAt)
                .ToListAsync();
        }

        public async Task<bool> IsUserInRoom(int roomId, int userId)
        {
            return await _context.RoomMembers.AnyAsync(member => member.RoomId == roomId && member.UserId == userId && member.IsActive);
        }

        public async Task<int> CountMembersByRoomId(int roomId)
        {
            return await _context.RoomMembers.CountAsync(member => member.RoomId == roomId && member.IsActive);
        }

        public async Task<List<ChatRoom>> FindPublicRooms()
        {
            return await _context.ChatRooms
                .Where(room => room.IsActive && room.RoomType == "PUBLIC")
                .OrderByDescending(room => room.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatRoom> AddRoom(ChatRoom room)
        {
            var entry = await _context.ChatRooms.AddAsync(room);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<RoomMember> AddMember(RoomMember member)
        {
            var entry = await _context.RoomMembers.AddAsync(member);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}

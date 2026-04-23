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
            return await _context.ChatRooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.IsActive);
        }

        public async Task<List<ChatRoom>> FindByCreatedBy(int createdBy)
        {
            return await _context.ChatRooms
                .Where(r => r.CreatedBy == createdBy && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatRoom?> FindByRoomName(string roomName)
        {
            return await _context.ChatRooms
                .FirstOrDefaultAsync(r => r.RoomName == roomName && r.IsActive);
        }

        public async Task<List<ChatRoom>> FindRoomsByUserId(int userId)
        {
            return await _context.RoomMembers
                .Where(m => m.UserId == userId && m.IsActive)
                .Join(_context.ChatRooms.Where(r => r.IsActive),
                    member => member.RoomId,
                    room => room.RoomId,
                    (_, room) => room)
                .Distinct()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RoomMember>> FindMembersByRoomId(int roomId)
        {
            return await _context.RoomMembers
                .Where(m => m.RoomId == roomId && m.IsActive)
                .OrderBy(m => m.JoinedAt)
                .ToListAsync();
        }

        public async Task<bool> IsUserInRoom(int roomId, int userId)
        {
            return await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);
        }

        public async Task<int> CountMembersByRoomId(int roomId)
        {
            return await _context.RoomMembers
                .CountAsync(m => m.RoomId == roomId && m.IsActive);
        }

        public async Task<List<ChatRoom>> FindPublicRooms()
        {
            return await _context.ChatRooms
                .Where(r => r.IsActive && r.RoomType == "PUBLIC")
                .OrderByDescending(r => r.CreatedAt)
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

        public Task RemoveMember(RoomMember member)
        {
            member.IsActive = false;
            return Task.CompletedTask;
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}

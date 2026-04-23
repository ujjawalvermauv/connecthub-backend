using ConnectHub.ChatHub.Models;
using ConnectHub.ChatHub.Repositories;

namespace ConnectHub.ChatHub.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private static readonly HashSet<string> AllowedRoomTypes = ["PUBLIC", "PRIVATE", "DIRECT"];
        private static readonly HashSet<string> AllowedRoles = ["ADMIN", "MODERATOR", "MEMBER"];

        private readonly IChatRoomRepository _chatRoomRepository;

        public ChatRoomService(IChatRoomRepository chatRoomRepository)
        {
            _chatRoomRepository = chatRoomRepository;
        }

        public async Task<ChatRoom> CreateRoom(ChatRoom room)
        {
            if (string.IsNullOrWhiteSpace(room.RoomName))
            {
                throw new ArgumentException("RoomName is required.");
            }

            if (room.MaxMembers <= 0)
            {
                room.MaxMembers = 500;
            }

            room.RoomType = room.RoomType.ToUpperInvariant();
            if (!AllowedRoomTypes.Contains(room.RoomType))
            {
                throw new ArgumentException("RoomType must be PUBLIC, PRIVATE, or DIRECT.");
            }

            room.CreatedAt = DateTime.UtcNow;
            room.IsActive = true;

            var createdRoom = await _chatRoomRepository.AddRoom(room);
            await AddMember(createdRoom.RoomId, room.CreatedBy, "ADMIN");
            return createdRoom;
        }

        public Task<ChatRoom?> GetRoomById(int roomId) => _chatRoomRepository.FindByRoomId(roomId);

        public Task<List<ChatRoom>> GetRoomsByUser(int userId) => _chatRoomRepository.FindRoomsByUserId(userId);

        public Task<List<ChatRoom>> GetPublicRooms() => _chatRoomRepository.FindPublicRooms();

        public async Task<ChatRoom?> UpdateRoom(int roomId, ChatRoom room)
        {
            var existing = await _chatRoomRepository.FindByRoomId(roomId);
            if (existing == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(room.RoomName))
            {
                existing.RoomName = room.RoomName;
            }

            existing.Description = room.Description;
            existing.AvatarUrl = room.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(room.RoomType))
            {
                var roomType = room.RoomType.ToUpperInvariant();
                if (!AllowedRoomTypes.Contains(roomType))
                {
                    throw new ArgumentException("RoomType must be PUBLIC, PRIVATE, or DIRECT.");
                }

                existing.RoomType = roomType;
            }

            if (room.MaxMembers > 0)
            {
                existing.MaxMembers = room.MaxMembers;
            }

            await _chatRoomRepository.SaveChanges();
            return existing;
        }

        public async Task DeleteRoom(int roomId)
        {
            var existing = await _chatRoomRepository.FindByRoomId(roomId);
            if (existing == null)
            {
                return;
            }

            existing.IsActive = false;
            await _chatRoomRepository.SaveChanges();
        }

        public async Task AddMember(int roomId, int userId, string role = "MEMBER")
        {
            var room = await _chatRoomRepository.FindByRoomId(roomId);
            if (room == null)
            {
                throw new InvalidOperationException("Room not found.");
            }

            if (await _chatRoomRepository.IsUserInRoom(roomId, userId))
            {
                return;
            }

            var currentMembers = await _chatRoomRepository.CountMembersByRoomId(roomId);
            if (currentMembers >= room.MaxMembers)
            {
                throw new InvalidOperationException("Room has reached the maximum member limit.");
            }

            var normalizedRole = role.ToUpperInvariant();
            if (!AllowedRoles.Contains(normalizedRole))
            {
                throw new ArgumentException("Role must be ADMIN, MODERATOR, or MEMBER.");
            }

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                Role = normalizedRole,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _chatRoomRepository.AddMember(member);
        }

        public async Task RemoveMember(int roomId, int userId)
        {
            var members = await _chatRoomRepository.FindMembersByRoomId(roomId);
            var member = members.FirstOrDefault(item => item.UserId == userId);
            if (member == null)
            {
                return;
            }

            member.IsActive = false;
            await _chatRoomRepository.SaveChanges();
        }

        public Task<List<RoomMember>> GetMembers(int roomId) => _chatRoomRepository.FindMembersByRoomId(roomId);

        public async Task UpdateMemberRole(int roomId, int userId, string role)
        {
            var normalizedRole = role.ToUpperInvariant();
            if (!AllowedRoles.Contains(normalizedRole))
            {
                throw new ArgumentException("Role must be ADMIN, MODERATOR, or MEMBER.");
            }

            var members = await _chatRoomRepository.FindMembersByRoomId(roomId);
            var member = members.FirstOrDefault(item => item.UserId == userId);
            if (member == null)
            {
                throw new InvalidOperationException("Member not found.");
            }

            member.Role = normalizedRole;
            await _chatRoomRepository.SaveChanges();
        }

        public Task LeaveRoom(int roomId, int userId) => RemoveMember(roomId, userId);

        public Task<bool> IsUserInRoom(int roomId, int userId) => _chatRoomRepository.IsUserInRoom(roomId, userId);

        public Task<int> GetMemberCount(int roomId) => _chatRoomRepository.CountMembersByRoomId(roomId);
    }
}

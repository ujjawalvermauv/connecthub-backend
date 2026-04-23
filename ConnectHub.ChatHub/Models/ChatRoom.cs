using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatHub.Models
{
    public class ChatRoom
    {
        [Key]
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RoomType { get; set; } = "PUBLIC";
        public string? AvatarUrl { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public int MaxMembers { get; set; } = 500;
        public List<RoomMember> Members { get; set; } = new();
    }
}

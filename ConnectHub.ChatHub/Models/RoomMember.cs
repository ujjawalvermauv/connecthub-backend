namespace ConnectHub.ChatHub.Models
{
    public class RoomMember
    {
        public int MemberId { get; set; }
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "MEMBER";
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ChatRoom? Room { get; set; }
    }
}

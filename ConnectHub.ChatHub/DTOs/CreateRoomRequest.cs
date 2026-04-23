namespace ConnectHub.ChatHub.DTOs
{
    public class CreateRoomRequest
    {
        public string RoomName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RoomType { get; set; } = "PUBLIC";
        public string? AvatarUrl { get; set; }
        public int CreatedBy { get; set; }
        public int MaxMembers { get; set; } = 500;
    }
}

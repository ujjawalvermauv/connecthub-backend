namespace ConnectHub.ChatHub.DTOs
{
    public class UpdateRoomRequest
    {
        public string? RoomName { get; set; }
        public string? Description { get; set; }
        public string? RoomType { get; set; }
        public string? AvatarUrl { get; set; }
        public int MaxMembers { get; set; }
    }
}

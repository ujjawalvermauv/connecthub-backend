namespace ConnectHub.ChatHub.DTOs
{
    public class AddMemberRequest
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "MEMBER";
    }
}

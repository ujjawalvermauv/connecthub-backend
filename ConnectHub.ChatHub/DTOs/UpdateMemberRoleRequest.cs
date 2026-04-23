namespace ConnectHub.ChatHub.DTOs
{
    public class UpdateMemberRoleRequest
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "MEMBER";
    }
}

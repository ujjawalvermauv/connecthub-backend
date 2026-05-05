namespace ConnectHub.ChatHub.Models
{
    public class UserConnection
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public string? DeviceInfo { get; set; }
    }
}

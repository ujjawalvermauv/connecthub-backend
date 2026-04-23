namespace ConnectHub.SignalR.Models
{
    public class UserConnection
    {
        public int UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public string? DeviceInfo { get; set; }
    }
}

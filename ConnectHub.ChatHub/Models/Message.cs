using System;

namespace ConnectHub.ChatHub.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public int? RoomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "TEXT";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

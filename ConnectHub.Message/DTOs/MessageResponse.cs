namespace ConnectHub.Message.DTOs
{
    public class MessageResponse
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public int? RoomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "TEXT";
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public string? MediaUrl { get; set; }
        public int? ReplyToMessageId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ConnectHub.Message.DTOs
{
    public class CreateRoomMessageRequest
    {
        [Range(1, int.MaxValue)]
        public int SenderId { get; set; }

        [Range(1, int.MaxValue)]
        public int RoomId { get; set; }

        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string MessageType { get; set; } = "TEXT";

        [StringLength(2048)]
        public string? MediaUrl { get; set; }

        [Range(1, int.MaxValue)]
        public int? ReplyToMessageId { get; set; }
    }
}

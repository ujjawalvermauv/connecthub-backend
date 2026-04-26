using Microsoft.AspNetCore.Http;

namespace ConnectHub.Media.DTOs
{
    public class UploadMediaRequest
    {
        public IFormFile File { get; set; } = default!;
        public int UploadedBy { get; set; }
        public int? MessageId { get; set; }
        public int? RoomId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}

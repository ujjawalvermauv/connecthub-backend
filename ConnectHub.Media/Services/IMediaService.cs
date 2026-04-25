using ConnectHub.Media.Models;

namespace ConnectHub.Media.Services
{
    public interface IMediaService
    {
        Task<MediaFile> UploadFile(IFormFile file, int uploadedBy, int? messageId = null, int? roomId = null, DateTime? expiresAt = null);
        Task<MediaFile?> GetFileById(string fileId);
        Task<List<MediaFile>> GetFilesByUser(int uploadedBy);
        Task<List<MediaFile>> GetFilesByRoom(int roomId);
        Task<List<MediaFile>> GetFilesByMessage(int messageId);
        Task DeleteFile(string fileId);
        Task<string> GenerateSasUrl(string fileId);
        Task CleanupExpiredFiles();
        Task<Dictionary<string, long>> GetFileStats();
    }
}

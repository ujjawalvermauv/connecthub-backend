using ConnectHub.Media.Models;

namespace ConnectHub.Media.Repositories
{
    public interface IMediaRepository
    {
        Task<MediaFile> AddAsync(MediaFile mediaFile);
        Task<MediaFile?> FindByFileId(string fileId);
        Task<List<MediaFile>> FindByUploadedBy(int uploadedBy);
        Task<List<MediaFile>> FindByMessageId(int messageId);
        Task<List<MediaFile>> FindByRoomId(int roomId);
        Task<bool> DeleteByFileId(string fileId);
        Task<List<MediaFile>> FindExpiredFiles(DateTime thresholdUtc);
        Task<List<MediaFile>> FindAll();
        Task<int> SaveChanges();
    }
}

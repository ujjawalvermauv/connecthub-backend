using ConnectHub.Media.Data;
using ConnectHub.Media.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.Media.Repositories
{
    public class MediaRepository : IMediaRepository
    {
        private readonly MediaDbContext _context;

        public MediaRepository(MediaDbContext context)
        {
            _context = context;
        }

        public async Task<MediaFile> AddAsync(MediaFile mediaFile)
        {
            var entry = await _context.MediaFiles.AddAsync(mediaFile);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<MediaFile?> FindByFileId(string fileId)
        {
            return await _context.MediaFiles.FirstOrDefaultAsync(mediaFile => mediaFile.FileId == fileId);
        }

        public async Task<List<MediaFile>> FindByUploadedBy(int uploadedBy)
        {
            return await _context.MediaFiles
                .Where(mediaFile => mediaFile.UploadedBy == uploadedBy)
                .OrderByDescending(mediaFile => mediaFile.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<MediaFile>> FindByMessageId(int messageId)
        {
            return await _context.MediaFiles
                .Where(mediaFile => mediaFile.MessageId == messageId)
                .OrderByDescending(mediaFile => mediaFile.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<MediaFile>> FindByRoomId(int roomId)
        {
            return await _context.MediaFiles
                .Where(mediaFile => mediaFile.RoomId == roomId)
                .OrderByDescending(mediaFile => mediaFile.UploadedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteByFileId(string fileId)
        {
            var mediaFile = await _context.MediaFiles.FirstOrDefaultAsync(file => file.FileId == fileId);
            if (mediaFile == null)
            {
                return false;
            }

            _context.MediaFiles.Remove(mediaFile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MediaFile>> FindExpiredFiles(DateTime thresholdUtc)
        {
            return await _context.MediaFiles
                .Where(mediaFile => mediaFile.ExpiresAt.HasValue && mediaFile.ExpiresAt.Value <= thresholdUtc)
                .OrderBy(mediaFile => mediaFile.ExpiresAt)
                .ToListAsync();
        }

        public async Task<List<MediaFile>> FindAll()
        {
            return await _context.MediaFiles
                .OrderByDescending(mediaFile => mediaFile.UploadedAt)
                .ToListAsync();
        }

        public async Task<int> SaveChanges()
        {
            return await _context.SaveChangesAsync();
        }
    }
}

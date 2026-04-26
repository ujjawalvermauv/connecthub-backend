using ConnectHub.Media.Models;
using ConnectHub.Media.Options;
using ConnectHub.Media.Repositories;
using Microsoft.Extensions.Options;
using MediaFileEntity = ConnectHub.Media.Models.MediaFile;

namespace ConnectHub.Media.Services
{
    public class MediaService : IMediaService
    {
        private readonly IMediaRepository _mediaRepository;
        private readonly IBlobStorageGateway _blobStorageGateway;
        private readonly AzureBlobOptions _blobOptions;

        public MediaService(
            IMediaRepository mediaRepository,
            IBlobStorageGateway blobStorageGateway,
            IOptions<AzureBlobOptions> blobOptions)
        {
            _mediaRepository = mediaRepository;
            _blobStorageGateway = blobStorageGateway;
            _blobOptions = blobOptions.Value;
            ValidateBlobOptions();
        }

        public async Task<MediaFileEntity> UploadFile(IFormFile file, int uploadedBy, int? messageId = null, int? roomId = null, DateTime? expiresAt = null)
        {
            if (file == null || file.Length <= 0)
            {
                throw new ArgumentException("A file is required for upload.");
            }

            if (uploadedBy <= 0)
            {
                throw new ArgumentException("UploadedBy must be a valid user id.");
            }

            var fileId = Guid.NewGuid().ToString("N");

            await using var fileStream = file.OpenReadStream();
            var resolvedContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;
            var blobUrl = await _blobStorageGateway.UploadAsync(_blobOptions.ContainerName, fileId, fileStream, resolvedContentType);

            var mediaFile = new MediaFileEntity
            {
                FileId = fileId,
                UploadedBy = uploadedBy,
                FileName = file.FileName,
                ContentType = resolvedContentType,
                FileSizeKb = Math.Max(1, (long)Math.Ceiling(file.Length / 1024d)),
                BlobUrl = blobUrl,
                ThumbnailUrl = null,
                MessageId = messageId,
                RoomId = roomId,
                UploadedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            return await _mediaRepository.AddAsync(mediaFile);
        }

        public Task<MediaFileEntity?> GetFileById(string fileId)
        {
            return _mediaRepository.FindByFileId(fileId);
        }

        public Task<List<MediaFileEntity>> GetFilesByUser(int uploadedBy)
        {
            return _mediaRepository.FindByUploadedBy(uploadedBy);
        }

        public Task<List<MediaFileEntity>> GetFilesByRoom(int roomId)
        {
            return _mediaRepository.FindByRoomId(roomId);
        }

        public Task<List<MediaFileEntity>> GetFilesByMessage(int messageId)
        {
            return _mediaRepository.FindByMessageId(messageId);
        }

        public async Task DeleteFile(string fileId)
        {
            var mediaFile = await _mediaRepository.FindByFileId(fileId);
            if (mediaFile == null)
            {
                return;
            }

            await _blobStorageGateway.DeleteIfExistsAsync(_blobOptions.ContainerName, mediaFile.FileId);
            await _mediaRepository.DeleteByFileId(fileId);
        }

        public async Task<string> GenerateSasUrl(string fileId)
        {
            var mediaFile = await _mediaRepository.FindByFileId(fileId);
            if (mediaFile == null)
            {
                throw new KeyNotFoundException($"Media file '{fileId}' was not found.");
            }

            return await _blobStorageGateway.GenerateReadSasUrlAsync(
                _blobOptions.ContainerName,
                mediaFile.FileId,
                DateTimeOffset.UtcNow.AddHours(1));
        }

        public async Task CleanupExpiredFiles()
        {
            var expiredFiles = await _mediaRepository.FindExpiredFiles(DateTime.UtcNow);
            if (expiredFiles.Count == 0)
            {
                return;
            }

            foreach (var expiredFile in expiredFiles)
            {
                await _blobStorageGateway.DeleteIfExistsAsync(_blobOptions.ContainerName, expiredFile.FileId);
                await _mediaRepository.DeleteByFileId(expiredFile.FileId);
            }
        }

        public async Task<Dictionary<string, long>> GetFileStats()
        {
            var mediaFiles = await _mediaRepository.FindAll();
            var expiredCount = mediaFiles.Count(mediaFile => mediaFile.ExpiresAt.HasValue && mediaFile.ExpiresAt.Value <= DateTime.UtcNow);

            return new Dictionary<string, long>
            {
                ["TotalFiles"] = mediaFiles.LongCount(),
                ["TotalSizeKb"] = mediaFiles.Sum(mediaFile => mediaFile.FileSizeKb),
                ["ExpiredFiles"] = expiredCount,
                ["ActiveFiles"] = mediaFiles.LongCount() - expiredCount,
                ["FilesWithThumbnails"] = mediaFiles.LongCount(mediaFile => !string.IsNullOrWhiteSpace(mediaFile.ThumbnailUrl))
            };
        }

        private void ValidateBlobOptions()
        {
            if (string.IsNullOrWhiteSpace(_blobOptions.ContainerName))
            {
                throw new InvalidOperationException("AzureBlob:ContainerName is not configured for ConnectHub.Media.");
            }
        }
    }
}

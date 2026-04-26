using ConnectHub.Media.Data;
using ConnectHub.Media.Models;
using ConnectHub.Media.Options;
using ConnectHub.Media.Repositories;
using ConnectHub.Media.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConnectHub.Media.Tests;

public class MediaServiceIntegrationTests
{
    [Fact]
    public async Task UploadFile_And_GenerateSasUrl_Work_EndToEnd()
    {
        var dbOptions = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var dbContext = new MediaDbContext(dbOptions);
        var repository = new MediaRepository(dbContext);
        var blobGateway = new FakeBlobStorageGateway();
        var mediaService = new MediaService(
            repository,
            blobGateway,
            Microsoft.Extensions.Options.Options.Create(new AzureBlobOptions
            {
                ContainerName = "media-files"
            }));

        await using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello-media"));
        IFormFile formFile = new FormFile(content, 0, content.Length, "file", "greeting.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploaded = await mediaService.UploadFile(formFile, uploadedBy: 101, messageId: 501, roomId: 601, expiresAt: DateTime.UtcNow.AddDays(1));
        var fromDb = await repository.FindByFileId(uploaded.FileId);
        var sasUrl = await mediaService.GenerateSasUrl(uploaded.FileId);

        Assert.NotNull(fromDb);
        Assert.Equal("text/plain", fromDb!.ContentType);
        Assert.Equal(101, fromDb.UploadedBy);
        Assert.Equal(501, fromDb.MessageId);
        Assert.Equal(601, fromDb.RoomId);
        Assert.Contains(uploaded.FileId, fromDb.BlobUrl, StringComparison.Ordinal);
        Assert.Contains("sig=", sasUrl, StringComparison.Ordinal);
        Assert.Contains(uploaded.FileId, sasUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CleanupExpiredFiles_Removes_Metadata_And_Blob()
    {
        var dbOptions = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var dbContext = new MediaDbContext(dbOptions);
        var repository = new MediaRepository(dbContext);
        var blobGateway = new FakeBlobStorageGateway();
        var mediaService = new MediaService(
            repository,
            blobGateway,
            Microsoft.Extensions.Options.Options.Create(new AzureBlobOptions
            {
                ContainerName = "media-files"
            }));

        await using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("expired-media"));
        IFormFile formFile = new FormFile(content, 0, content.Length, "file", "expired.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploaded = await mediaService.UploadFile(
            formFile,
            uploadedBy: 202,
            expiresAt: DateTime.UtcNow.AddMinutes(-5));

        var beforeCleanup = await repository.FindByFileId(uploaded.FileId);
        Assert.NotNull(beforeCleanup);

        await mediaService.CleanupExpiredFiles();

        var afterCleanup = await repository.FindByFileId(uploaded.FileId);
        Assert.Null(afterCleanup);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            blobGateway.GenerateReadSasUrlAsync("media-files", uploaded.FileId, DateTimeOffset.UtcNow.AddHours(1)));
    }

    private sealed class FakeBlobStorageGateway : IBlobStorageGateway
    {
        private readonly Dictionary<string, string> _blobContentTypes = new(StringComparer.Ordinal);

        public Task<string> UploadAsync(
            string containerName,
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            _blobContentTypes[blobName] = contentType;
            return Task.FromResult($"https://local.blob/{containerName}/{blobName}");
        }

        public Task DeleteIfExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            _blobContentTypes.Remove(blobName);
            return Task.CompletedTask;
        }

        public Task<string> GenerateReadSasUrlAsync(
            string containerName,
            string blobName,
            DateTimeOffset expiresOn,
            CancellationToken cancellationToken = default)
        {
            if (!_blobContentTypes.ContainsKey(blobName))
            {
                throw new KeyNotFoundException($"Blob '{blobName}' does not exist.");
            }

            var escapedExpiry = Uri.EscapeDataString(expiresOn.ToString("O"));
            return Task.FromResult($"https://local.blob/{containerName}/{blobName}?se={escapedExpiry}&sig=fake-signature");
        }
    }
}

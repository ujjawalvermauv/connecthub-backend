namespace ConnectHub.Media.Services
{
    public interface IBlobStorageGateway
    {
        Task<string> UploadAsync(
            string containerName,
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default);

        Task DeleteIfExistsAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default);

        Task<string> GenerateReadSasUrlAsync(
            string containerName,
            string blobName,
            DateTimeOffset expiresOn,
            CancellationToken cancellationToken = default);
    }
}
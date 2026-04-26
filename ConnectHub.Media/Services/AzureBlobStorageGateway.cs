using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ConnectHub.Media.Options;
using Microsoft.Extensions.Options;

namespace ConnectHub.Media.Services
{
    public class AzureBlobStorageGateway : IBlobStorageGateway
    {
        private readonly AzureBlobOptions _blobOptions;
        private BlobServiceClient? _blobServiceClient;

        public AzureBlobStorageGateway(IOptions<AzureBlobOptions> blobOptions)
        {
            _blobOptions = blobOptions.Value;
        }

        public async Task<string> UploadAsync(
            string containerName,
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            await containerClient.UploadBlobAsync(blobName, content, cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            }, cancellationToken: cancellationToken);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteIfExistsAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            await containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        public async Task<string> GenerateReadSasUrlAsync(
            string containerName,
            string blobName,
            DateTimeOffset expiresOn,
            CancellationToken cancellationToken = default)
        {
            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Blob client cannot generate SAS URIs with the configured credentials.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = expiresOn
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        private async Task<BlobContainerClient> GetContainerClientAsync(string containerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_blobOptions.ConnectionString))
            {
                throw new InvalidOperationException("AzureBlob:ConnectionString is not configured for ConnectHub.Media.");
            }

            _blobServiceClient ??= new BlobServiceClient(_blobOptions.ConnectionString);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return containerClient;
        }
    }
}
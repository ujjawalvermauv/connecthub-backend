using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace ConnectHub.Media.Services
{
    public class BlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration config)
        {
            // Connection string should be set using .NET User Secrets for local development
            // and Azure App Service Configuration or environment variables in production.
            _connectionString = config["AzureBlob:ConnectionString"] ?? config["BlobStorage:ConnectionString"];
            _containerName = config["AzureBlob:ContainerName"] ?? config["BlobStorage:ContainerName"];
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("AzureBlob:ConnectionString (or BlobStorage:ConnectionString) is not configured. Set it using .NET User Secrets for local development or Azure App Service Configuration in production.");
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);
            return blobClient.Uri.ToString();
        }
    }
}
using ConnectHub.Media.Options;
using Microsoft.Extensions.Options;

namespace ConnectHub.Media.Services
{
    public sealed class LocalBlobStorageGateway : IBlobStorageGateway
    {
        private readonly IWebHostEnvironment _environment;
        private readonly MediaStorageOptions _storageOptions;
        private readonly ILogger<LocalBlobStorageGateway> _logger;

        public LocalBlobStorageGateway(
            IWebHostEnvironment environment,
            IOptions<MediaStorageOptions> storageOptions,
            ILogger<LocalBlobStorageGateway> logger)
        {
            _environment = environment;
            _storageOptions = storageOptions.Value;
            _logger = logger;
        }

        public async Task<string> UploadAsync(
            string containerName,
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var uploadsRoot = ResolveUploadsRoot(containerName);
            Directory.CreateDirectory(uploadsRoot);

            var filePath = Path.Combine(uploadsRoot, blobName);
            await using var fileStream = File.Create(filePath);
            await content.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("Stored media file locally at {FilePath}", filePath);

            return BuildPublicUrl(containerName, blobName);
        }

        public Task DeleteIfExistsAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            var uploadsRoot = ResolveUploadsRoot(containerName);
            var filePath = Path.Combine(uploadsRoot, blobName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }

        public Task<string> GenerateReadSasUrlAsync(
            string containerName,
            string blobName,
            DateTimeOffset expiresOn,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BuildPublicUrl(containerName, blobName));
        }

        private string ResolveUploadsRoot(string containerName)
        {
            var rootFolder = _storageOptions.UploadFolder;
            var uploadsRoot = Path.IsPathRooted(rootFolder)
                ? rootFolder
                : Path.Combine(_environment.ContentRootPath, rootFolder);

            if (ShouldAppendContainer(uploadsRoot, containerName))
            {
                uploadsRoot = Path.Combine(uploadsRoot, containerName);
            }

            return uploadsRoot;
        }

        private string BuildPublicUrl(string containerName, string blobName)
        {
            var baseUrl = (_storageOptions.PublicBaseUrl ?? string.Empty).TrimEnd('/');

            var uploadFolder = (_storageOptions.UploadFolder ?? "wwwroot/uploads").Replace('\\','/').Trim();
            var publicPath = uploadFolder.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase)
                ? uploadFolder.Substring("wwwroot".Length)
                : uploadFolder.StartsWith("/") ? uploadFolder : "/" + uploadFolder;

            if (!publicPath.StartsWith("/")) publicPath = "/" + publicPath;

            var requestPath = !ShouldAppendContainer(uploadFolder, containerName)
                ? $"{publicPath}/{blobName}".Replace("//","/")
                : $"{publicPath}/{containerName}/{blobName}".Replace("//","/");

            return string.IsNullOrWhiteSpace(baseUrl)
                ? requestPath
                : $"{baseUrl}{requestPath}";
        }

        private static bool ShouldAppendContainer(string rootPath, string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return false;
            }

            var normalized = rootPath.Replace('\\', '/').TrimEnd('/');
            return !normalized.EndsWith("/" + containerName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(normalized, containerName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
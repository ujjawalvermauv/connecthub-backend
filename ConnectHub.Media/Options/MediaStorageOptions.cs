namespace ConnectHub.Media.Options
{
    public class MediaStorageOptions
    {
        public string Provider { get; set; } = "Local";
        public string ContainerName { get; set; } = "uploads";
        public string UploadFolder { get; set; } = "wwwroot/uploads";
        public string PublicBaseUrl { get; set; } = "http://localhost:5005";
        public long MaxUploadBytes { get; set; } = 100 * 1024 * 1024;
    }
}
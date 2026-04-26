namespace ConnectHub.Media.Options
{
    public class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "media-files";
    }
}

dotnet add package Azure.Storage.Blobsdotnet add package Azure.Storage.Blobsdotnet add package Azure.Storage.Blobsnamespace ConnectHub.Auth.Models
{
    public class User
    {
        public int UserId { get; set; }
<<<<<<< HEAD
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
=======
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
>>>>>>> media-service
        public string Role { get; set; } = "User";
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
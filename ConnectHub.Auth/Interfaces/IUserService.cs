using ConnectHub.Auth.Models;

namespace ConnectHub.Auth.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterAsync(User user);
        Task<string> LoginAsync(string email, string password);
        Task<List<User>> SearchUsersAsync(string query);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> UpdateProfileAsync(int userId, User profile);
        Task<string> GoogleLoginAsync(string idToken);
    }
}

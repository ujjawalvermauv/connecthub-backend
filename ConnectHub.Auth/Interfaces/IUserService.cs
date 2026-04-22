using ConnectHub.Auth.Models;

namespace ConnectHub.Auth.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterAsync(User user);
        Task<string> LoginAsync(string email, string password);
    }
}

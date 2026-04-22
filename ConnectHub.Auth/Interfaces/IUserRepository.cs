using ConnectHub.Auth.Models;

namespace ConnectHub.Auth.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByUserNameAsync(string userName);
        Task AddUserAsync(User user);
    }
}

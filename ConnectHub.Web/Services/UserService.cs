using ConnectHub.Web.Models;
using System.Collections.Generic;

namespace ConnectHub.Web.Services
{
    public interface IUserService
    {
        User GetUserById(int id);
        IEnumerable<User> SearchUsers(string query);
        void Register(User user);
        void EditProfile(int id, User updatedUser);
    }

    public class UserService : IUserService
    {
        public User GetUserById(int id)
        {
            // TODO: Implement DB logic
            return new User { Id = id, Username = "DemoUser" };
        }
        public IEnumerable<User> SearchUsers(string query)
        {
            // TODO: Implement search logic
            return new List<User>();
        }
        public void Register(User user)
        {
            // TODO: Implement registration logic
        }
        public void EditProfile(int id, User updatedUser)
        {
            // TODO: Implement profile update logic
        }
    }
}

using ConnectHub.Auth.Data;
using ConnectHub.Auth.Interfaces;
using ConnectHub.Auth.Models;
using Microsoft.AspNetCore.Identity;

namespace ConnectHub.Auth.Services
{
    public class UserService : IUserService
    {
        private readonly AuthDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(AuthDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User> RegisterAsync(User user)
        {
            // Hash the password
            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            // Set default values
            user.IsActive = true;
            user.IsOnline = false;
            user.CreatedAt = DateTime.UtcNow;
            user.LastSeen = DateTime.UtcNow;

            // Add user to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            // Find user by email
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Invalid password.");

            // Update last seen
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Return dummy JWT token (to be replaced with real JWT later)
            return "JWT_TOKEN_HERE";
        }
    }
}

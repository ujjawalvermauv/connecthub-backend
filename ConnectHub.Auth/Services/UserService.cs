using ConnectHub.Auth.Data;
using ConnectHub.Auth.Interfaces;
using ConnectHub.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            user.Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
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

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKey123"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

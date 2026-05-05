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

        // 🔥 Strong key (32+ chars)
        private const string JwtKey = "ThisIsAReallyStrongSecretKeyForJWTAuth123456";

        public UserService(AuthDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User> RegisterAsync(User user)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            user.Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
            user.IsActive = true;
            user.IsOnline = false;
            user.CreatedAt = DateTime.UtcNow;
            user.LastSeen = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Invalid password.");

            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                 new(ClaimTypes.NameIdentifier, user.UserId.ToString()),

    // 🔥 IMPORTANT FIX
                new("userName", user.UserName),
                new("displayName", string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName),

                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            // ✅ FIXED KEY
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
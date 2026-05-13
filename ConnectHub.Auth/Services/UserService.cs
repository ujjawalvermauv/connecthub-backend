using ConnectHub.Auth.Data;
using ConnectHub.Auth.Interfaces;
using ConnectHub.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
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
            user.UserName = user.UserName.Trim();
            user.DisplayName = user.DisplayName.Trim();
            user.Email = user.Email.Trim().ToLowerInvariant();
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
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

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

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return await GetAllUsersAsync();

            return await _context.Users
                .Where(u => u.IsActive && (u.UserName.Contains(query) || u.DisplayName.Contains(query) || u.Email.Contains(query)))
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
        }

        public async Task<User?> UpdateProfileAsync(int userId, User profile)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            user.DisplayName = profile.DisplayName;
            user.Email = profile.Email;
            user.Bio = profile.Bio;
            user.AvatarUrl = profile.AvatarUrl;

            await _context.SaveChangesAsync();
            return user;
        }

        public Task<string> GoogleLoginAsync(string idToken)
        {
            return GoogleLoginInternalAsync(idToken);
        }

        private async Task<string> GoogleLoginInternalAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentException("Google ID token is required.", nameof(idToken));

            var parts = idToken.Split('.');
            if (parts.Length < 2)
                throw new InvalidOperationException("Invalid Google ID token.");

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var document = JsonDocument.Parse(payloadJson);
            var payload = document.RootElement;

            var email = payload.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Google token does not contain an email address.");

            var displayName = payload.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() ?? string.Empty
                : string.Empty;

            var picture = payload.TryGetProperty("picture", out var pictureProp)
                ? pictureProp.GetString()
                : null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                user = new User
                {
                    UserName = email.Split('@')[0],
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName,
                    Email = email,
                    PasswordHash = string.Empty,
                    AvatarUrl = picture,
                    ProfilePictureUrl = picture,
                    Role = "User",
                    IsActive = true,
                    IsOnline = true,
                    CreatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }
            else
            {
                user.IsActive = true;
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    user.DisplayName = displayName;
                }

                if (!string.IsNullOrWhiteSpace(picture))
                {
                    user.AvatarUrl = picture;
                    user.ProfilePictureUrl = picture;
                }
            }

            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("userName", user.UserName),
                new Claim("displayName", string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var padded = input.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            return Convert.FromBase64String(padded);
        }
    }
}
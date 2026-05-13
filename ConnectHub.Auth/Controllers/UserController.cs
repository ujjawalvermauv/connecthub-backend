using ConnectHub.Auth.Interfaces;
using ConnectHub.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConnectHub.Auth.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Server-side validation: email is required
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }

                var user = new User
                {
                    UserName = request.UserName,
                    DisplayName = request.DisplayName,
                    Email = request.Email,
                    PasswordHash = request.Password
                };

                var registeredUser = await _userService.RegisterAsync(user);

                return CreatedAtAction(nameof(Register), new { id = registeredUser.UserId }, registeredUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Login a user
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _userService.LoginAsync(request.Email, request.Password);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Message = "Login successful"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Search for users
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<User>>> Search([FromQuery] string query)
        {
            try
            {
                var users = await _userService.SearchUsersAsync(query);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get profile for authenticated user
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetProfile()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid token: user id claim missing" });
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                user.UserId,
                user.UserName,
                user.DisplayName,
                user.Email,
                user.Role,
                user.AvatarUrl,
                user.ProfilePictureUrl,
                user.Bio,
                user.IsOnline,
                user.LastSeen,
                user.CreatedAt,
                user.IsActive
            });
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("update/{userId}")]
        public async Task<ActionResult<User>> UpdateProfile(int userId, [FromBody] User profile)
        {
            try
            {
                if (profile == null)
                {
                    return BadRequest(new { message = "Profile cannot be null" });
                }
                // Prevent accidentally clearing the email to empty string
                if (profile.Email != null && string.IsNullOrWhiteSpace(profile.Email))
                {
                    return BadRequest(new { message = "Email cannot be empty" });
                }
                var updatedUser = await _userService.UpdateProfileAsync(userId, profile);
                if (updatedUser == null) return NotFound();
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Upload avatar (Simplified)
        /// </summary>
        [HttpPost("{userId}/avatar")]
        public async Task<ActionResult> UploadAvatar(int userId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("No file uploaded");

                // Path to wwwroot/avatars
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var avatarsPath = Path.Combine(wwwrootPath, "avatars");
                
                if (!Directory.Exists(avatarsPath))
                {
                    Directory.CreateDirectory(avatarsPath);
                }

                // Generate a unique filename to prevent caching issues
                var extension = Path.GetExtension(file.FileName);
                var fileName = $"user_{userId}_{DateTime.Now.Ticks}{extension}";
                var filePath = Path.Combine(avatarsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // The URL path should match the StaticFile mapping in Program.cs
                var avatarUrl = $"/api/users/avatars/{fileName}";
                
                // Update user record in DB
                var profile = new User { AvatarUrl = avatarUrl };
                await _userService.UpdateProfileAsync(userId, profile);
                
                return Ok(new { url = avatarUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove avatar
        /// </summary>
        [HttpDelete("{userId}/avatar")]
        public async Task<ActionResult> RemoveAvatar(int userId)
        {
            try
            {
                var profile = new User { AvatarUrl = null };
                await _userService.UpdateProfileAsync(userId, profile);
                return Ok(new { message = "Avatar removed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // DTOs
    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

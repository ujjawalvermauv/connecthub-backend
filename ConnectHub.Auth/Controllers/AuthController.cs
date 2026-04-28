using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using ConnectHub.Auth.Data;
using ConnectHub.Auth.Models;

namespace ConnectHub.Auth.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _db;
        private readonly IConfiguration _config;
        private readonly Microsoft.IdentityModel.Tokens.SecurityKey _signingKey;

        public AuthController(AuthDbContext db, IConfiguration config, Microsoft.IdentityModel.Tokens.SecurityKey signingKey)
        {
            _db = db;
            _config = config;
            _signingKey = signingKey;
        }

        [HttpGet("google")]
        public IActionResult Google()
        {
            var clientId = _config["GoogleAuth:ClientId"];
            var redirect = _config["GoogleAuth:RedirectUri"];
            var scope = "openid email profile";
            var url = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirect)}&scope={Uri.EscapeDataString(scope)}&access_type=offline&prompt=consent";
            return Redirect(url);
        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest(new { message = "Missing code" });

            var clientId = _config["GoogleAuth:ClientId"];
            var clientSecret = _config["GoogleAuth:ClientSecret"];
            var redirect = _config["GoogleAuth:RedirectUri"];

            using var http = new HttpClient();

            var tokenReq = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirect,
                ["grant_type"] = "authorization_code"
            };

            var tokenResp = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(tokenReq));
            if (!tokenResp.IsSuccessStatusCode)
            {
                var body = await tokenResp.Content.ReadAsStringAsync();
                return StatusCode((int)tokenResp.StatusCode, new { message = "Token exchange failed", detail = body });
            }

            var tokenJson = await tokenResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(tokenJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id_token", out var idTokenProp))
                return BadRequest(new { message = "id_token missing from token response" });

            var idToken = idTokenProp.GetString();

            // Decode id_token payload
            var parts = idToken.Split('.');
            if (parts.Length < 2) return BadRequest(new { message = "Invalid id_token" });
            var payload = parts[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var payloadJson = Encoding.UTF8.GetString(bytes);
            using var pj = JsonDocument.Parse(payloadJson);
            var pjroot = pj.RootElement;
            var email = pjroot.GetProperty("email").GetString() ?? string.Empty;
            var name = pjroot.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;

            // Find or create user
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    UserName = email.Split('@')[0],
                    DisplayName = name,
                    Email = email,
                    PasswordHash = string.Empty,
                    IsActive = true,
                    IsOnline = true,
                    CreatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            // Issue JWT
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            // Use signing key provided via DI (validated at startup)
            var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(12), signingCredentials: creds);
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Redirect back to frontend with token as fragment (or return JSON depending on flow)
            var frontend = _config["Frontend:Url"] ?? "http://localhost:4200";
            var redirectUrl = $"{frontend}/auth/callback#token={token}";
            return Redirect(redirectUrl);
        }
    }
}

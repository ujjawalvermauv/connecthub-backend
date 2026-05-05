using ConnectHub.ChatHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.ChatHub.Controllers
{
    [ApiController]
    [Route("api/presence")]
    [Authorize]
    public class PresenceController : ControllerBase
    {
        private readonly IPresenceService _presenceService;

        public PresenceController(IPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        // GET api/presence/online-users
        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
            => Ok(_presenceService.GetOnlineUserIds());

        // GET api/presence/is-online/5
        [HttpGet("is-online/{userId:int}")]
        public IActionResult IsUserOnline(int userId)
            => Ok(new { userId, isOnline = _presenceService.IsUserOnline(userId) });

        // GET api/presence/connection-count
        [HttpGet("connection-count")]
        public IActionResult GetConnectionCount()
            => Ok(new { connectionCount = _presenceService.GetConnectionCount() });

        // GET api/presence/online-users-info  (admin only)
        [HttpGet("online-users-info")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOnlineUsersInfo()
            => Ok(_presenceService.GetOnlineUsersInfo());
    }
}

using ConnectHub.SignalR.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.SignalR.Controllers
{
    [ApiController]
    [Route("api/presence")]
    public class PresenceController : ControllerBase
    {
        private readonly IPresenceService _presenceService;

        public PresenceController(IPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
        {
            return Ok(_presenceService.GetOnlineUserIds());
        }

        [HttpGet("online-users-info")]
        public IActionResult GetOnlineUsersInfo()
        {
            return Ok(_presenceService.GetOnlineUsersInfo());
        }

        [HttpGet("is-online/{userId:int}")]
        public IActionResult IsOnline([FromRoute] int userId)
        {
            return Ok(new
            {
                userId,
                isOnline = _presenceService.IsUserOnline(userId),
                connectionCount = _presenceService.GetConnectionCount(userId)
            });
        }

        [HttpDelete("clear")]
        public IActionResult Clear([FromQuery] int? userId)
        {
            _presenceService.ClearUserConnections(userId);
            return NoContent();
        }
    }
}

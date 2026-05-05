using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.ChatHub.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomController : ControllerBase
    {
        private static readonly List<object> _rooms = new()
        {
            new { id = 1, name = "General Discussion", description = "Talk about anything!", memberCount = 120, icon = "groups" },
            new { id = 2, name = "Tech Talk", description = "Latest in software and gadgets.", memberCount = 85, icon = "computer" },
            new { id = 3, name = "Gaming Zone", description = "Find players and discuss games.", memberCount = 210, icon = "sports_esports" }
        };

        [HttpGet]
        public IActionResult GetRooms()
        {
            return Ok(_rooms);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetRoom(int id)
        {
            var room = _rooms.FirstOrDefault(r => (int)((dynamic)r).id == id);
            return room != null ? Ok(room) : NotFound();
        }
    }
}

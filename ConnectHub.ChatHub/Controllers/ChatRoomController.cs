using ConnectHub.ChatHub.DTOs;
using ConnectHub.ChatHub.Models;
using ConnectHub.ChatHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.ChatHub.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class ChatRoomController : ControllerBase
    {
        private readonly IChatRoomService _roomService;

        public ChatRoomController(IChatRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            try
            {
                var room = new ChatRoom
                {
                    RoomName = request.RoomName,
                    Description = request.Description,
                    RoomType = request.RoomType,
                    AvatarUrl = request.AvatarUrl,
                    CreatedBy = request.CreatedBy,
                    MaxMembers = request.MaxMembers
                };

                var createdRoom = await _roomService.CreateRoom(room);
                return CreatedAtAction(nameof(GetRoomById), new { roomId = createdRoom.RoomId }, createdRoom);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("addMember")]
        public async Task<IActionResult> AddMember([FromBody] AddMemberRequest request)
        {
            try
            {
                await _roomService.AddMember(request.RoomId, request.UserId, request.Role);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{roomId:int}")]
        public async Task<IActionResult> GetRoomById([FromRoute] int roomId)
        {
            var room = await _roomService.GetRoomById(roomId);
            return room == null ? NotFound() : Ok(room);
        }

        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetRoomsByUser([FromRoute] int userId)
        {
            var rooms = await _roomService.GetRoomsByUser(userId);
            return Ok(rooms);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicRooms()
        {
            var rooms = await _roomService.GetPublicRooms();
            return Ok(rooms);
        }

        [HttpGet("members/{roomId:int}")]
        public async Task<IActionResult> GetMembers([FromRoute] int roomId)
        {
            var members = await _roomService.GetMembers(roomId);
            return Ok(members);
        }

        [HttpPut("{roomId:int}")]
        public async Task<IActionResult> UpdateRoom([FromRoute] int roomId, [FromBody] UpdateRoomRequest request)
        {
            try
            {
                var room = new ChatRoom
                {
                    RoomName = request.RoomName ?? string.Empty,
                    Description = request.Description,
                    RoomType = request.RoomType ?? string.Empty,
                    AvatarUrl = request.AvatarUrl,
                    MaxMembers = request.MaxMembers
                };

                var updatedRoom = await _roomService.UpdateRoom(roomId, room);
                return updatedRoom == null ? NotFound() : Ok(updatedRoom);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("memberRole")]
        public async Task<IActionResult> UpdateMemberRole([FromBody] UpdateMemberRoleRequest request)
        {
            try
            {
                await _roomService.UpdateMemberRole(request.RoomId, request.UserId, request.Role);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{roomId:int}")]
        public async Task<IActionResult> DeleteRoom([FromRoute] int roomId)
        {
            await _roomService.DeleteRoom(roomId);
            return NoContent();
        }

        [HttpDelete("removeMember")]
        public async Task<IActionResult> RemoveMember([FromBody] AddMemberRequest request)
        {
            await _roomService.RemoveMember(request.RoomId, request.UserId);
            return NoContent();
        }

        [HttpDelete("leave")]
        public async Task<IActionResult> LeaveRoom([FromBody] LeaveRoomRequest request)
        {
            await _roomService.LeaveRoom(request.RoomId, request.UserId);
            return NoContent();
        }
    }
}

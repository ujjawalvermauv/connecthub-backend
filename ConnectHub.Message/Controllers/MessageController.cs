using ConnectHub.Message.DTOs;
using ConnectHub.Message.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.Message.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("direct")]
        public async Task<IActionResult> GetDirectMessages([FromQuery] int senderId, [FromQuery] int receiverId)
        {
            var messages = await _messageService.GetDirectMessages(senderId, receiverId);
            return Ok(messages);
        }

        [HttpGet("room/{roomId:int}")]
        public async Task<IActionResult> GetRoomMessages([FromRoute] int roomId)
        {
            var messages = await _messageService.GetRoomMessages(roomId);
            return Ok(messages);
        }

        [HttpGet("unread/{receiverId:int}")]
        public async Task<IActionResult> GetUnread([FromRoute] int receiverId)
        {
            var messages = await _messageService.GetUnreadMessages(receiverId);
            return Ok(messages);
        }

        [HttpGet("unread/count/{receiverId:int}")]
        public async Task<IActionResult> GetUnreadCount([FromRoute] int receiverId)
        {
            var count = await _messageService.GetUnreadCount(receiverId);
            return Ok(count);
        }

        [HttpGet("recent/{userId:int}")]
        public async Task<IActionResult> GetRecentChats([FromRoute] int userId)
        {
            var messages = await _messageService.GetRecentChats(userId);
            return Ok(messages);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] string query, [FromQuery] int userId)
        {
            var messages = await _messageService.SearchMessages(query, userId);
            return Ok(messages);
        }

        [HttpGet("by-room/{roomId:int}")]
        public async Task<IActionResult> GetMessagesByRoom([FromRoute] int roomId)
        {
            var messages = await _messageService.GetMessagesByRoom(roomId);
            return Ok(messages);
        }

        [HttpPut("mark-read/{messageId:int}")]
        public async Task<IActionResult> MarkAsRead([FromRoute] int messageId)
        {
            await _messageService.MarkAsRead(messageId);
            return NoContent();
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] int receiverId, [FromQuery] int? roomId)
        {
            await _messageService.MarkAllAsRead(receiverId, roomId);
            return NoContent();
        }

        [HttpPut("edit/{messageId:int}")]
        public async Task<IActionResult> EditMessage([FromRoute] int messageId, [FromBody] EditMessageRequest request)
        {
            try
            {
                var updatedMessage = await _messageService.EditMessage(messageId, request.Content);
                if (updatedMessage == null)
                {
                    return NotFound();
                }

                return Ok(updatedMessage);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{messageId:int}")]
        public async Task<IActionResult> DeleteMessage([FromRoute] int messageId)
        {
            await _messageService.DeleteMessage(messageId);
            return NoContent();
        }
    }
}

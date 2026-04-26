using ConnectHub.Media.DTOs;
using ConnectHub.Media.Models;
using ConnectHub.Media.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.Media.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadMediaRequest request)
        {
            try
            {
                var mediaFile = await _mediaService.UploadFile(
                    request.File,
                    request.UploadedBy,
                    request.MessageId,
                    request.RoomId,
                    request.ExpiresAt);

                return CreatedAtAction(nameof(GetById), new { fileId = mediaFile.FileId }, mediaFile);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetById([FromRoute] string fileId)
        {
            var mediaFile = await _mediaService.GetFileById(fileId);
            return mediaFile == null ? NotFound() : Ok(mediaFile);
        }

        [HttpGet("byUser/{uploadedBy:int}")]
        public async Task<IActionResult> GetByUser([FromRoute] int uploadedBy)
        {
            var mediaFiles = await _mediaService.GetFilesByUser(uploadedBy);
            return Ok(mediaFiles);
        }

        [HttpGet("byRoom/{roomId:int}")]
        public async Task<IActionResult> GetByRoom([FromRoute] int roomId)
        {
            var mediaFiles = await _mediaService.GetFilesByRoom(roomId);
            return Ok(mediaFiles);
        }

        [HttpGet("byMessage/{messageId:int}")]
        public async Task<IActionResult> GetByMessage([FromRoute] int messageId)
        {
            var mediaFiles = await _mediaService.GetFilesByMessage(messageId);
            return Ok(mediaFiles);
        }

        [HttpGet("sasUrl/{fileId}")]
        public async Task<IActionResult> GetSasUrl([FromRoute] string fileId)
        {
            try
            {
                var sasUrl = await _mediaService.GenerateSasUrl(fileId);
                return Ok(new { sasUrl });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> Delete([FromRoute] string fileId)
        {
            try
            {
                await _mediaService.DeleteFile(fileId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _mediaService.GetFileStats();
            return Ok(stats);
        }
    }
}

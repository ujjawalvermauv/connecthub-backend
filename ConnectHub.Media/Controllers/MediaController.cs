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
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded." });
                }

                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName =
                    Guid.NewGuid().ToString() + "_" + file.FileName;

                var filePath = Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl =
                    $"{Request.Scheme}://{Request.Host}/uploads/{uniqueFileName}";

                return Ok(new
                {
                    success = true,
                    url = fileUrl,
                    fileName = uniqueFileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
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

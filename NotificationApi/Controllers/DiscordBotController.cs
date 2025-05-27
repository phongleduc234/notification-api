using Microsoft.AspNetCore.Mvc;
using NotificationApi.Models;
using NotificationApi.Services;
using System.ComponentModel.DataAnnotations;

namespace NotificationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DiscordBotController : ControllerBase
    {
        private readonly IDiscordBotService _discordBotService;
        private readonly ILogger<DiscordBotController> _logger;

        public DiscordBotController(
            IDiscordBotService discordBotService,
            ILogger<DiscordBotController> logger)
        {
            _discordBotService = discordBotService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi tin nhắn văn bản đơn giản đến Discord channel
        /// </summary>
        /// <param name="request">Thông tin tin nhắn cần gửi</param>
        /// <returns>Kết quả gửi tin nhắn</returns>
        /// <response code="200">Gửi tin nhắn thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("send-message")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.Error("Invalid request data", errors));
                }

                await _discordBotService.SendMessageAsync(request.Message);
                return Ok(ApiResponse<object>.Ok(null, "Message sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Discord");
                return StatusCode(500, ApiResponse<object>.Error("Failed to send message"));
            }
        }

        /// <summary>
        /// Gửi tin nhắn dạng embed đến Discord channel
        /// </summary>
        /// <param name="request">Thông tin tin nhắn embed cần gửi</param>
        /// <returns>Kết quả gửi tin nhắn</returns>
        /// <response code="200">Gửi tin nhắn thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("send-embed")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> SendEmbed([FromBody] SendEmbedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.Error("Invalid request data", errors));
                }

                await _discordBotService.SendEmbedMessageAsync(
                    request.Title,
                    request.Description,
                    request.Color
                );
                return Ok(ApiResponse<object>.Ok(null, "Embed message sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending embed message to Discord");
                return StatusCode(500, ApiResponse<object>.Error("Failed to send embed message"));
            }
        }
    }

    public class SendMessageRequest
    {
        [Required(ErrorMessage = "Message is required")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string Message { get; set; } = string.Empty;
    }

    public class SendEmbedRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(256, ErrorMessage = "Title cannot exceed 256 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(4096, ErrorMessage = "Description cannot exceed 4096 characters")]
        public string Description { get; set; } = string.Empty;

        [RegularExpression(@"^0x[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in format 0xRRGGBB")]
        public string Color { get; set; } = "0x00ff00";
    }
} 
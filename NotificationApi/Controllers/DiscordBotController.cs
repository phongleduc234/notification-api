using Microsoft.AspNetCore.Mvc;
using NotificationApi.Services;
using System.Threading.Tasks;

namespace NotificationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                await _discordBotService.SendMessageAsync(request.Message);
                return Ok(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Discord");
                return StatusCode(500, new { success = false, message = "Failed to send message" });
            }
        }

        /// <summary>
        /// Gửi tin nhắn dạng embed đến Discord channel
        /// </summary>
        [HttpPost("send-embed")]
        public async Task<IActionResult> SendEmbed([FromBody] SendEmbedRequest request)
        {
            try
            {
                await _discordBotService.SendEmbedMessageAsync(
                    request.Title,
                    request.Description,
                    request.Color
                );
                return Ok(new { success = true, message = "Embed message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending embed message to Discord");
                return StatusCode(500, new { success = false, message = "Failed to send embed message" });
            }
        }
    }

    public class SendMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class SendEmbedRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "0x00ff00";
    }
} 
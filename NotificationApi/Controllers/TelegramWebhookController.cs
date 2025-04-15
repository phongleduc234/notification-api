// Controllers/TelegramWebhookController.cs
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NotificationApi.Models;
using NotificationApi.Services;

namespace NotificationApi.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly ITelegramBotService _telegramService;
        private readonly ILogger<TelegramWebhookController> _logger;

        public TelegramWebhookController(
            ITelegramBotService telegramService,
            ILogger<TelegramWebhookController> logger)
        {
            _telegramService = telegramService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement update)
        {
            try
            {
                _logger.LogInformation("Received Telegram update: {Update}", 
                    JsonSerializer.Serialize(update));

                // Xử lý cập nhật từ Telegram ở đây
                // Ví dụ: Trích xuất tin nhắn và phản hồi
                if (update.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("text", out var textElement) &&
                    messageElement.TryGetProperty("chat", out var chatElement) &&
                    chatElement.TryGetProperty("id", out var chatIdElement))
                {
                    var text = textElement.GetString();
                    var chatId = chatIdElement.GetInt64();
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Ví dụ đơn giản: echo lại tin nhắn
                        await _telegramService.SendMessageAsync($"You said: {text}", (int)chatId);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Telegram webhook");
                return Ok(); // Vẫn trả về 200 OK theo yêu cầu của Telegram API
            }
        }

        [HttpGet("setup-webhook")]
        public async Task<ActionResult<ApiResponse<bool>>> SetupWebhook([FromQuery] string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                // Nếu không có baseUrl được cung cấp, thử lấy từ request
                baseUrl = $"{Request.Scheme}://{Request.Host}";
            }

            var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/telegram/webhook";
            var result = await _telegramService.SetWebhookAsync(webhookUrl);
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Webhook successfully setup at {webhookUrl}",
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to setup webhook",
                    Data = false
                });
            }
        }

        [HttpGet("remove-webhook")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveWebhook()
        {
            var result = await _telegramService.DeleteWebhookAsync();
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Webhook successfully removed",
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to remove webhook",
                    Data = false
                });
            }
        }

        [HttpGet("send")]
        public async Task<ActionResult<ApiResponse<bool>>> SendMessage([FromQuery] string message = "Test message from API")
        {
            var result = await _telegramService.SendMessageAsync(message);
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Message sent successfully",
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to send message",
                    Data = false
                });
            }
        }
    }
}


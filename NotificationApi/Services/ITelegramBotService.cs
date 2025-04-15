// Services/TelegramBotService.cs
using System.Text.Json;

namespace NotificationApi.Services
{
    public interface ITelegramBotService
    {
        Task<bool> SetWebhookAsync(string url);
        Task<bool> DeleteWebhookAsync();
        Task<bool> SendMessageAsync(string text, int? chatId = null);
    }
    
    public class TelegramBotOptions
    {
        public string BotToken { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
    }

    public class TelegramBotService : ITelegramBotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly string _botToken;
        private readonly string _defaultChatId;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        public TelegramBotService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TelegramBotService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TelegramBot");
            _logger = logger;
            
            var telegramConfig = configuration.GetSection("Telegram");
            _botToken = telegramConfig["BotToken"] ?? throw new ArgumentNullException("BotToken is missing in configuration");
            _defaultChatId = telegramConfig["ChatId"] ?? string.Empty;
            
            _httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{_botToken}/");
        }

        public async Task<bool> SetWebhookAsync(string url)
        {
            try
            {
                var requestUrl = $"setWebhook?url={Uri.EscapeDataString(url)}";
                var response = await _httpClient.GetAsync(requestUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TelegramResponse>(content, _jsonOptions);
                    
                    if (result?.Ok == true)
                    {
                        _logger.LogInformation("Telegram webhook successfully set to {Url}", url);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to set Telegram webhook: {Description}", result?.Description);
                    }
                }
                else
                {
                    _logger.LogError("Error setting Telegram webhook: {StatusCode}", response.StatusCode);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when setting Telegram webhook");
                return false;
            }
        }

        public async Task<bool> DeleteWebhookAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("deleteWebhook");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TelegramResponse>(content, _jsonOptions);
                    
                    if (result?.Ok == true)
                    {
                        _logger.LogInformation("Telegram webhook successfully deleted");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete Telegram webhook: {Description}", result?.Description);
                    }
                }
                else
                {
                    _logger.LogError("Error deleting Telegram webhook: {StatusCode}", response.StatusCode);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when deleting Telegram webhook");
                return false;
            }
        }

        public async Task<bool> SendMessageAsync(string text, int? chatId = null)
        {
            try
            {
                var messageData = new
                {
                    chat_id = chatId?.ToString() ?? _defaultChatId,
                    text = text,
                    parse_mode = "HTML"
                };

                var response = await _httpClient.PostAsJsonAsync("sendMessage", messageData);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TelegramResponse>(content, _jsonOptions);
                    
                    if (result?.Ok == true)
                    {
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send Telegram message: {Description}", result?.Description);
                    }
                }
                else
                {
                    _logger.LogError("Error sending Telegram message: {StatusCode}", response.StatusCode);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when sending Telegram message");
                return false;
            }
        }
    }
    public class TelegramResponse
    {
        public bool Ok { get; set; }
        public string? Result { get; set; }
        public string? Description { get; set; }
    }
}


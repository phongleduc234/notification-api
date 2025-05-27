using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotificationApi.Services
{
    public interface IDiscordBotService
    {
        Task SendMessageAsync(string message);
        Task SendEmbedMessageAsync(string title, string description, string color = "0x00ff00");
    }

    public class DiscordBotService : IDiscordBotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly string _channelId;
        private readonly ILogger<DiscordBotService> _logger;
        private const string DiscordApiBaseUrl = "https://discord.com/api/v10";

        public DiscordBotService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DiscordBotService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("DiscordBot");
            _botToken = configuration["Discord:BotToken"] ?? throw new ArgumentNullException("Discord:BotToken");
            _channelId = configuration["Discord:ChannelId"] ?? throw new ArgumentNullException("Discord:ChannelId");
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_botToken}");
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                var payload = new
                {
                    content = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{DiscordApiBaseUrl}/channels/{_channelId}/messages",
                    content);

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Discord");
                throw;
            }
        }

        public async Task SendEmbedMessageAsync(string title, string description, string color = "0x00ff00")
        {
            try
            {
                var payload = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = title,
                            description = description,
                            color = Convert.ToInt32(color.Replace("0x", ""), 16)
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{DiscordApiBaseUrl}/channels/{_channelId}/messages",
                    content);

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending embed message to Discord");
                throw;
            }
        }
    }
} 
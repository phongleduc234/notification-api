using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NotificationApi.Services
{
    public interface IDiscordBotService
    {
        Task SendMessageAsync(string message);
        Task SendEmbedMessageAsync(string title, string description, string color = "0x00ff00");
        Task InitializeAsync();
    }

    public class DiscordBotService : IDiscordBotService
    {
        private readonly DiscordSocketClient _client;
        private readonly string _botToken;
        private readonly ulong _channelId;
        private readonly ILogger<DiscordBotService> _logger;
        private bool _isInitialized;

        public DiscordBotService(
            IConfiguration configuration,
            ILogger<DiscordBotService> logger)
        {
            _botToken = configuration["Discord:BotToken"] ?? throw new ArgumentNullException("Discord:BotToken");
            _channelId = ulong.Parse(configuration["Discord:ChannelId"] ?? throw new ArgumentNullException("Discord:ChannelId"));
            _logger = logger;

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | 
                                GatewayIntents.GuildMessages | 
                                GatewayIntents.MessageContent |
                                GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = false,
                ConnectionTimeout = 30000,
                MessageCacheSize = 100,
                LogLevel = LogSeverity.Info,
                UseSystemClock = true,
                HandlerTimeout = 30000,
                ShardId = 0,
                TotalShards = 1
            };

            _client = new DiscordSocketClient(config);
            _client.Log += LogAsync;
            _client.Disconnected += async (exception) =>
            {
                _logger.LogWarning(exception, "Discord client disconnected. Attempting to reconnect...");
                await Task.Delay(5000); // Đợi 5 giây trước khi thử kết nối lại
                try
                {
                    await _client.StartAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect Discord client");
                }
            };
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                await _client.LoginAsync(TokenType.Bot, _botToken);
                await _client.StartAsync();
                _isInitialized = true;
                _logger.LogInformation("Discord bot initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Discord bot");
                throw;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                var channel = await _client.GetChannelAsync(_channelId) as IMessageChannel;
                if (channel == null)
                {
                    throw new InvalidOperationException($"Could not find channel with ID {_channelId}");
                }

                await channel.SendMessageAsync(message);
                _logger.LogInformation("Message sent successfully to Discord");
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
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                var channel = await _client.GetChannelAsync(_channelId) as IMessageChannel;
                if (channel == null)
                {
                    throw new InvalidOperationException($"Could not find channel with ID {_channelId}");
                }

                var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription(description)
                    .WithColor(Convert.ToUInt32(color.Replace("0x", ""), 16))
                    .WithCurrentTimestamp()
                    .Build();

                await channel.SendMessageAsync(embed: embed);
                _logger.LogInformation("Embed message sent successfully to Discord");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending embed message to Discord");
                throw;
            }
        }

        private Task LogAsync(LogMessage log)
        {
            var logLevel = log.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, log.Exception, "[Discord] {Message}", log.Message);
            return Task.CompletedTask;
        }
    }
} 
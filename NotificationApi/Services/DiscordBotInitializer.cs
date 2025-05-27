using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationApi.Services
{
    public class DiscordBotInitializer : BackgroundService
    {
        private readonly IDiscordBotService _discordBotService;
        private readonly ILogger<DiscordBotInitializer> _logger;

        public DiscordBotInitializer(
            IDiscordBotService discordBotService,
            ILogger<DiscordBotInitializer> logger)
        {
            _discordBotService = discordBotService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Initializing Discord bot...");
                await _discordBotService.InitializeAsync();
                _logger.LogInformation("Discord bot initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Discord bot");
                throw;
            }
        }
    }
} 
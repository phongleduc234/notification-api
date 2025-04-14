// Services/TelegramWebhookInitializer.cs
namespace NotificationApi.Services
{
    public class TelegramWebhookInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramWebhookInitializer> _logger;
        private readonly IConfiguration _configuration;

        public TelegramWebhookInitializer(
            IServiceProvider serviceProvider,
            ILogger<TelegramWebhookInitializer> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing Telegram webhook...");
            
            // Kiểm tra có nên tự động thiết lập webhook hay không
            var telegramSection = _configuration.GetSection("Telegram");
            if (!telegramSection.GetValue<bool>("AutoSetWebhook", false))
            {
                _logger.LogInformation("Automatic webhook setup is disabled");
                return;
            }
            
            using var scope = _serviceProvider.CreateScope();
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
            
            var baseUrl = telegramSection["WebhookBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("Webhook base URL not configured, cannot set webhook automatically");
                return;
            }
            
            var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/telegram/webhook";
            var success = await telegramService.SetWebhookAsync(webhookUrl);
            
            if (success)
            {
                _logger.LogInformation("Telegram webhook successfully set to {Url}", webhookUrl);
            }
            else
            {
                _logger.LogWarning("Failed to set Telegram webhook");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


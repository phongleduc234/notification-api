// Services/DailyCounterResetService.cs
namespace MaiApi.Services
{
    public class DailyCounterResetService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyCounterResetService> _logger;

        public DailyCounterResetService(
            IServiceProvider serviceProvider,
            ILogger<DailyCounterResetService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate time until midnight
                var now = DateTime.UtcNow;
                var midnight = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
                var timeUntilMidnight = midnight - now;

                // Sleep until midnight
                _logger.LogInformation("Daily counter reset scheduled in {Hours} hours", timeUntilMidnight.TotalHours);
                await Task.Delay(timeUntilMidnight, stoppingToken);

                // Reset counters
                using var scope = _serviceProvider.CreateScope();
                var emailUserService = scope.ServiceProvider.GetRequiredService<IEmailUserService>();
                await emailUserService.ResetDailyCountersAsync();
            }
        }
    }
}

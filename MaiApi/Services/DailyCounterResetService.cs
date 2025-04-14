// Services/DailyCounterResetService.cs
using StackExchange.Redis;
using Microsoft.Extensions.Options;

namespace MaiApi.Services
{
    public class DailyCounterResetService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyCounterResetService> _logger;
        private readonly ConnectionMultiplexer _redis;
        private readonly string _lockKey = "email_api:daily_counter_reset_lock";
        private readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(5); // Lock expires after 5 minutes
        private readonly string _instanceId = Guid.NewGuid().ToString(); // Unique ID for this service instance

        public DailyCounterResetService(
            IServiceProvider serviceProvider,
            ILogger<DailyCounterResetService> logger,
            ConnectionMultiplexer redis)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _redis = redis;
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

                // Try to acquire distributed lock
                IDatabase db = _redis.GetDatabase();
                bool lockAcquired = false;

                try
                {
                    // Attempt to acquire lock
                    lockAcquired = await db.StringSetAsync(
                        _lockKey,
                        _instanceId,
                        _lockTimeout,
                        When.NotExists);

                    if (lockAcquired)
                    {
                        _logger.LogInformation("Lock acquired by instance {InstanceId}. Performing daily counter reset.", _instanceId);

                        // Reset counters
                        using var scope = _serviceProvider.CreateScope();
                        var emailUserService = scope.ServiceProvider.GetRequiredService<IEmailUserService>();
                        await emailUserService.ResetDailyCountersAsync();

                        _logger.LogInformation("Daily counter reset completed by instance {InstanceId}", _instanceId);
                    }
                    else
                    {
                        _logger.LogInformation("Lock already acquired by another instance. Skipping daily counter reset.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during daily counter reset");
                }
                finally
                {
                    // Release the lock if we acquired it
                    if (lockAcquired)
                    {
                        // Only delete the key if it still contains our instance ID (using Lua script for atomicity)
                        string script = @"
                            if redis.call('get', KEYS[1]) == ARGV[1] then
                                return redis.call('del', KEYS[1])
                            else
                                return 0
                            end";

                        var result = await db.ScriptEvaluateAsync(
                            script,
                            new RedisKey[] { _lockKey },
                            new RedisValue[] { _instanceId });

                        _logger.LogInformation("Lock released by instance {InstanceId}", _instanceId);
                    }
                }

                // Add a small delay to prevent high CPU usage in case of errors
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}

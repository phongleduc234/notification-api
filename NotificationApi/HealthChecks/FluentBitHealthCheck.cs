using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NotificationApi.HealthChecks
{
    public class FluentBitHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public FluentBitHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var fluentBit = _configuration.GetSection("FluentBit");
                var host = fluentBit["Host"] ?? "localhost";
                var port = fluentBit["Port"] ?? "24225";
                var healthPort = fluentBit["ServicePort"] ?? "2020";

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"http://{host}:{healthPort}/api/v1/health", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Fluent Bit is healthy");
                }

                return HealthCheckResult.Unhealthy("Fluent Bit health check failed");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Fluent Bit health check failed", ex);
            }
        }
    }
} 
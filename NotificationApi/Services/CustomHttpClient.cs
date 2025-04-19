// Custom HTTP client for Fluent Bit integration
using Serilog.Sinks.Http;

public class CustomHttpClient : IHttpClient
{
    private readonly HttpClient _httpClient;

    public CustomHttpClient()
    {
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public void Configure(IConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream, CancellationToken cancellationToken)
    {
        var content = new StreamContent(contentStream);
        content.Headers.Add("Content-Type", "application/json");

        return await _httpClient.PostAsync(requestUri, content, cancellationToken);
    }
}

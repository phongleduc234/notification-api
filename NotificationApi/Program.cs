// Program.cs

using Microsoft.EntityFrameworkCore;
using NotificationApi.Data;
using NotificationApi.Repositories;
using NotificationApi.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using NotificationApi.Middleware;
using StackExchange.Redis;
using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Http.BatchFormatters;

var serviceName = "Notification API Service";
var serviceVersion = "1.0.0";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var serviceUrl = builder.Configuration.GetSection("ServiceUrl");
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion));
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(serviceUrl["OpenTelemetry"]);
        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    });
});

// Serilog config — enrich log with trace/span ID and send logs to Fluent Bit
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithSpan() // 👈 Thêm trace_id, span_id
    .Enrich.WithProperty("service", serviceName)
    .Enrich.WithProperty("version", serviceVersion)
    .Enrich.WithProperty("environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Http(
        requestUri: serviceUrl["FluentBit"],
        queueLimitBytes: null, // Set to null or a specific value as per your requirements
        batchFormatter: new ArrayBatchFormatter(),
        httpClient: new CustomHttpClient())
    .CreateLogger();

builder.Host.UseSerilog();

// Add this with the other service registrations
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
});

// Configure JSON options for controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký dịch vụ khởi tạo webhook
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITelegramBotService, TelegramBotService>();
builder.Services.AddHostedService<TelegramWebhookInitializer>();

// Thêm cấu hình OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.request.headers", string.Join(",", request.Headers.Select(h => $"{h.Key}={h.Value}")));
            };
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                activity.SetTag("http.response.headers", string.Join(",", response.Headers.Select(h => $"{h.Key}={h.Value}")));
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(serviceUrl["OpenTelemetry"]); // Địa chỉ OpenTelemetry Collector
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(serviceUrl["OpenTelemetry"]); // Địa chỉ OpenTelemetry Collector
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        }));


// Add Redis connection
builder.Services.AddSingleton(sp =>
{
    var redisConfig = builder.Configuration.GetSection("Redis");
    var host = redisConfig["Host"] ?? "localhost";
    var port = redisConfig["Port"] ?? "6379";
    var password = redisConfig["Password"] ?? "";

    var configOptions = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        ConnectTimeout = 5000
    };

    configOptions.EndPoints.Add($"{host}:{port}");

    if (!string.IsNullOrEmpty(password))
    {
        configOptions.Password = password;
    }
    return ConnectionMultiplexer.Connect(configOptions);
});


builder.Services.AddScoped<IEmailUserRepository, EmailUserRepository>();
builder.Services.AddScoped<IEmailUserService, EmailUserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add a background service to reset daily counters
builder.Services.AddHostedService<DailyCounterResetService>();

// Add health checks
builder.Services.AddHealthChecks();

// Add controllers
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification API",
        Version = "v1",
        Description = "API for notification management",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "jun8124@gmail.com"
        }
    });

    // Add XML comments support
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Group endpoints by controller
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
});

var app = builder.Build();
// Apply migrations automatically in development
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Mail API V1");
    c.RoutePrefix = "swagger";
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseExceptionMiddleware();
app.MapHealthChecks("/health");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();

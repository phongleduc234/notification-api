// Middleware/ExceptionMiddleware.cs
using System.Net;
using System.Text.Json;
using MaiApi.Models;

namespace MaiApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            string message = "An unexpected error occurred. Please try again later.";

            if (exception is ApiException apiException)
            {
                statusCode = (HttpStatusCode)apiException.StatusCode;
                message = apiException.Message;
            }
            else if (exception is InvalidOperationException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
            }

            _logger.LogError(exception, "Error: {Message}", exception.Message);

            context.Response.StatusCode = (int)statusCode;

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                // Only show stack trace in development
                Data = _environment.IsDevelopment() ? exception.StackTrace : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}

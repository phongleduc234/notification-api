// Models/ApiException.cs
namespace NotificationApi.Models
{
    public class ApiException : Exception
    {
        public int StatusCode { get; set; } = 400; // Default to Bad Request
        
        public ApiException(string message) : base(message)
        {
        }
        
        public ApiException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}

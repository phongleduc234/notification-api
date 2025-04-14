// Models/EmailUser.cs
namespace MaiApi.Models
{
    public class EmailUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastResetDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public int DailyEmailLimit { get; set; } = 100;
        public int EmailsSentToday { get; set; } = 0;
    }

    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}

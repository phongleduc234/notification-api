// Services/IEmailUserService.cs
using MaiApi.Models;
using MaiApi.Repositories;

namespace MaiApi.Services
{
    public interface IEmailUserService
    {
        Task<EmailUser> CreateUserAsync(string username, string email);
        Task<EmailUser?> GetUserByApiKeyAsync(string apiKey);
        Task<bool> ValidateUserAsync(string apiKey);
        Task<bool> UpdateEmailSentCountAsync(string userId);
        Task<bool> ResetDailyCountersAsync();
        Task<List<EmailUser>> GetAllUsersAsync();
    }

    public class EmailUserService : IEmailUserService
    {
        private readonly ILogger<EmailUserService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailUserRepository _repository;

        public EmailUserService(
            ILogger<EmailUserService> logger,
            IConfiguration configuration,
            IEmailUserRepository repository)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
        }

        public async Task<EmailUser> CreateUserAsync(string username, string email)
        {
            // Check if user already exists
            var existingByUsername = await _repository.GetByUsernameAsync(username);
            if (existingByUsername != null)
            {
                throw new InvalidOperationException($"Username '{username}' is already taken");
            }

            var existingByEmail = await _repository.GetByEmailAsync(email);
            if (existingByEmail != null)
            {
                throw new InvalidOperationException($"Email '{email}' is already registered");
            }

            var apiKey = GenerateApiKey();
            var user = new EmailUser
            {
                Username = username,
                Email = email,
                ApiKey = apiKey
            };

            await _repository.CreateAsync(user);
            _logger.LogInformation("Created new email API user: {Username}", username);
            
            return user;
        }

        public async Task<EmailUser?> GetUserByApiKeyAsync(string apiKey)
        {
            return await _repository.GetByApiKeyAsync(apiKey);
        }

        public async Task<bool> ValidateUserAsync(string apiKey)
        {
            var user = await GetUserByApiKeyAsync(apiKey);
            if (user == null) return false;
            
            // Check if daily counters need to be reset
            if (DateTime.UtcNow.Date > user.LastResetDate.Date)
            {
                user.EmailsSentToday = 0;
                user.LastResetDate = DateTime.UtcNow;
                await _repository.UpdateAsync(user);
            }
            
            return user.IsActive && user.EmailsSentToday < user.DailyEmailLimit;
        }

        public async Task<bool> UpdateEmailSentCountAsync(string userId)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null) return false;
            
            user.EmailsSentToday++;
            _logger.LogInformation("User {Username} has sent {Count} emails today", user.Username, user.EmailsSentToday);
            
            return await _repository.UpdateAsync(user);
        }

        public async Task<bool> ResetDailyCountersAsync()
        {
            var users = await _repository.GetAllAsync();
            bool success = true;
            
            foreach (var user in users)
            {
                user.EmailsSentToday = 0;
                user.LastResetDate = DateTime.UtcNow;
                success = success && await _repository.UpdateAsync(user);
            }
            
            _logger.LogInformation("Daily email counters have been reset for {Count} users", users.Count);
            return success;
        }

        public async Task<List<EmailUser>> GetAllUsersAsync()
        {
            return await _repository.GetAllAsync();
        }

        private string GenerateApiKey()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 32);
        }
    }
}

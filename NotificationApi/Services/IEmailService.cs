// Services/IEmailService.cs
using System.Net;
using System.Net.Mail;
using NotificationApi.Models;

namespace NotificationApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string apiKey, EmailRequest request);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailUserService _userService;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration,
            IEmailUserService userService)
        {
            _logger = logger;
            _configuration = configuration;
            _userService = userService;
        }

        public async Task<bool> SendEmailAsync(string apiKey, EmailRequest request)
        {
            // Validate apiKey
            if (!await _userService.ValidateUserAsync(apiKey))
            {
                _logger.LogWarning("Invalid API key or user has reached their daily limit");
                return false;
            }

            var user = await _userService.GetUserByApiKeyAsync(apiKey);
            if (user == null) return false;

            try
            {
                var smtpSettings = _configuration.GetSection("SmtpMail");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"]);
                var userName = smtpSettings["UserName"];
                var password = smtpSettings["Password"];
                var fromName = smtpSettings["FromName"] ?? "DevOps";

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(userName, password)
                };

                using var message = new MailMessage
                {
                    // From with display name
                    From = new MailAddress(userName, fromName),

                    Subject = request.Subject,
                    Body = request.Body,
                    IsBodyHtml = request.IsHtml
                };

                message.To.Add(request.To);

                if (request.Cc != null && request.Cc.Any(x => !string.IsNullOrWhiteSpace(x)))
                {
                    foreach (var cc in request.Cc)
                    {
                        message.CC.Add(cc);
                    }
                }

                if (request.Bcc != null && request.Bcc.Any(x => !string.IsNullOrWhiteSpace(x)))
                {
                    foreach (var bcc in request.Bcc)
                    {
                        message.Bcc.Add(bcc);
                    }
                }

                await client.SendMailAsync(message);

                // Update the counter for the user
                await _userService.UpdateEmailSentCountAsync(user.Id);

                _logger.LogInformation("Email sent successfully to {Recipient} by user {Username}",
                    request.To, user.Username);

                return true;
            }
            catch (ApiException)
            {
                // Re-throw ApiException to maintain the custom error message
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} by user {Username}",
                    request.To, user.Username);
                return false;
            }
        }
    }
}

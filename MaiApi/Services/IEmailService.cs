// Services/IEmailService.cs
using MaiApi.Models;

namespace MaiApi.Services
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
            if (!await _userService.ValidateUserAsync(apiKey))
            {
                _logger.LogWarning("Invalid API key or user has reached their daily limit");
                return false;
            }

            var user = await _userService.GetUserByApiKeyAsync(apiKey);
            if (user == null) return false;

            try
            {
                // Here we would use the SMTP settings from appsettings.json
                // but adapt them for this specific user's context

                var smtpSettings = _configuration.GetSection("SmtpMail");

                using var client = new System.Net.Mail.SmtpClient(
                    smtpSettings["Host"],
                    int.Parse(smtpSettings["Port"]));

                var credentials = new System.Net.NetworkCredential(
                    smtpSettings["User"],
                    smtpSettings["Password"]);

                client.Credentials = credentials;
                client.EnableSsl = false;

                var message = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(
                        smtpSettings["FromEmail"],
                        smtpSettings["FromName"]),
                    Subject = request.Subject,
                    Body = request.Body,
                    IsBodyHtml = request.IsHtml
                };

                message.To.Add(request.To);

                if (request.Cc != null)
                {
                    foreach (var cc in request.Cc)
                    {
                        message.CC.Add(cc);
                    }
                }

                if (request.Bcc != null)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} by user {Username}",
                    request.To, user.Username);
                return false;
            }
        }
    }
}

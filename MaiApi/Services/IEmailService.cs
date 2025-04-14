// Services/IEmailService.cs
using System.Net.Mail;
using System.Text.RegularExpressions;
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

        // Regular expression for validating email format
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$",
            RegexOptions.Compiled);

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration,
            IEmailUserService userService)
        {
            _logger = logger;
            _configuration = configuration;
            _userService = userService;
        }

        // Validate if the given string is a valid email format
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Try with MailAddress for primary validation
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                // If MailAddress parsing fails, use regex as backup
                return EmailRegex.IsMatch(email);
            }
        }

        // Validate the entire email request
        private (bool IsValid, string ErrorMessage) ValidateEmailRequest(EmailRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.To))
                return (false, "Recipient email is required");

            if (string.IsNullOrWhiteSpace(request.Subject))
                return (false, "Email subject is required");

            // Validate primary recipient
            if (!IsValidEmail(request.To))
                return (false, $"Invalid recipient email format: {request.To}");

            // Validate CC recipients
            if (request.Cc != null && request.Cc.Any())
            {
                foreach (var cc in request.Cc)
                {
                    if (!IsValidEmail(cc))
                        return (false, $"Invalid CC email format: {cc}");
                }
            }

            // Validate BCC recipients
            if (request.Bcc != null && request.Bcc.Any())
            {
                foreach (var bcc in request.Bcc)
                {
                    if (!IsValidEmail(bcc))
                        return (false, $"Invalid BCC email format: {bcc}");
                }
            }

            return (true, string.Empty);
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

            // Validate email request
            var (isValid, errorMessage) = ValidateEmailRequest(request);
            if (!isValid)
            {
                _logger.LogWarning("Email validation failed: {ErrorMessage} for user {Username}",
                    errorMessage, user.Username);
                throw new ApiException(errorMessage, 400);
            }

            try
            {
                // Here we would use the SMTP settings from appsettings.json
                // but adapt them for this specific user's context
                var smtpSettings = _configuration.GetSection("SmtpMail");

                using var client = new SmtpClient(
                    smtpSettings["Host"],
                    int.Parse(smtpSettings["Port"]));

                var credentials = new System.Net.NetworkCredential(
                    smtpSettings["User"],
                    smtpSettings["Password"]);

                client.Credentials = credentials;
                client.EnableSsl = false;

                var message = new MailMessage
                {
                    From = new MailAddress(
                        smtpSettings["User"],
                        smtpSettings["FromName"] ?? "DevOps"),
                    Subject = request.Subject,
                    Body = request.Body,
                    IsBodyHtml = request.IsHtml
                };

                message.To.Add(request.To);

                if (request.Cc != null && request.Cc.Any())
                {
                    foreach (var cc in request.Cc)
                    {
                        message.CC.Add(cc);
                    }
                }

                if (request.Bcc != null && request.Bcc.Any())
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

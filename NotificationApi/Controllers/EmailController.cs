// Controllers/EmailUsersController.cs
using Microsoft.AspNetCore.Mvc;
using NotificationApi.Models;
using NotificationApi.Services;

namespace NotificationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IEmailService emailService,
            ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<bool>>> SendEmail([FromHeader] string apiKey, [FromBody] EmailRequest request)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "API key is required"
                });
            }

            if (string.IsNullOrEmpty(request.To) || string.IsNullOrEmpty(request.Subject))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Recipient and subject are required"
                });
            }

            var result = await _emailService.SendEmailAsync(apiKey, request);
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Email sent successfully",
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to send email. Check API key or daily limit"
                });
            }
        }
    }
}

// Controllers/EmailUsersController.cs
using Microsoft.AspNetCore.Mvc;
using MaiApi.Services;
using MaiApi.Models;

namespace MaiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailUsersController : ControllerBase
    {
        private readonly IEmailUserService _userService;
        private readonly ILogger<EmailController> _logger;

        public EmailUsersController(
            IEmailUserService userService,
            ILogger<EmailController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<EmailUser>>> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new ApiResponse<EmailUser>
                {
                    Success = false,
                    Message = "Username and email are required"
                });
            }

            var user = await _userService.CreateUserAsync(request.Username, request.Email);

            return Ok(new ApiResponse<EmailUser>
            {
                Success = true,
                Message = "User created successfully",
                Data = user
            });
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
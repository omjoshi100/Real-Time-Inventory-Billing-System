using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeInventoryBilling.API.Data;
using RealTimeInventoryBilling.API.Services;
using RealTimeInventoryBilling.Shared.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IInventoryRepository _repository;
        private readonly JwtTokenService _jwtService;

        public AuthController(IInventoryRepository repository, JwtTokenService jwtService)
        {
            _repository = repository;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required." });
            }

            var user = await _repository.AuthenticateUserAsync(request.Username, request.Password);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid credentials or account is inactive." });
            }

            var (token, expiresAt) = _jwtService.GenerateToken(user);

            var response = new AuthResponseDto(
                token,
                user.UserId,
                user.Username,
                user.FullName,
                user.RoleName,
                expiresAt
            );

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var fullName = User.FindFirstValue("FullName");

            return Ok(new
            {
                UserId = userId,
                Username = username,
                FullName = fullName,
                Role = role
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using RiskAnalytics.Api.RequestModels;
using RiskAnalytics.Api.ResponseModels;
using RiskAnalytics.Api.Utilities;
using RiskAnalytics.Core.Cache;
using RiskAnalytics.Core.RiskEngine;

namespace RiskAnalytics.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRiskEngine _riskEngine;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IRiskEngine riskEngine, IRedisCacheService redisCacheService, ILogger<AuthController> logger)
        {
            _riskEngine = riskEngine;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            _logger.LogInformation("Login attempt for user: {Username}", loginRequest.Username);

            string cacheKey = $"user-auth:{loginRequest.Username}:{loginRequest.Password}";
            int? cachedUserId = await _redisCacheService.GetAsync<int?>(cacheKey);

            int userId;
            if (cachedUserId.HasValue)
            {
                userId = cachedUserId.Value;
            }
            else
            {
                userId = await _riskEngine.CheckUserAuthAsync(loginRequest.Username, loginRequest.Password);
                if (userId > 0)
                {
                    await _redisCacheService.SetAsync(cacheKey, userId, TimeSpan.FromHours(1));
                }
            }

            if (userId > 0)
            {
                var token = TokenUtility.GenerateJwtToken(userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new { token, userId }
                });
            }

            return Unauthorized(new ApiResponse
            {
                Success = false,
                Message = "Invalid credentials"
            });
        }

        [HttpDelete("delete")]
        public async Task<ActionResult<ApiResponse>> DeleteUser([FromBody] LoginRequest deleteRequest)
        {
            int userId = await _riskEngine.CheckUserAuthAsync(deleteRequest.Username, deleteRequest.Password);
            if (userId <= 0)
                return Unauthorized(new ApiResponse { Success = false, Message = "Invalid credentials" });

            await _riskEngine.DeleteUserAsync(userId);
            await _redisCacheService.DeleteAsync($"user-auth:{deleteRequest.Username}:{deleteRequest.Password}");

            return Ok(new ApiResponse { Success = true, Message = "User deleted successfully" });
        }

        [HttpPut("password")]
        public async Task<ActionResult<ApiResponse>> UpdatePassword([FromBody] LoginRequest request)
        {
            int userId = await _riskEngine.CheckUserAuthAsync(request.Username, request.Password);
            if (userId <= 0)
                return Unauthorized(new ApiResponse { Success = false, Message = "Invalid credentials" });

            bool success = await _riskEngine.UpdatePasswordAsync(userId, request.NewPassword!);
            if (success)
            {
                await _redisCacheService.DeleteAsync($"user-auth:{request.Username}:{request.Password}");
                return Ok(new ApiResponse { Success = true, Message = "Password updated successfully" });
            }

            return BadRequest(new ApiResponse { Success = false, Message = "Failed to update password" });
        }

        [HttpGet("user/{username}/id")]
        public async Task<ActionResult<ApiResponse<int>>> GetUserId(string username)
        {
            int userId = await _riskEngine.GetUserIdByUsernameAsync(username);
            if (userId <= 0)
                return NotFound(new ApiResponse<int> { Success = false, Message = "User not found" });

            return Ok(new ApiResponse<int> { Success = true, Data = userId });
        }
    }
}
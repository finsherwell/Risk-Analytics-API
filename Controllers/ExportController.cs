using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskAnalytics.Core.Cache;
using RiskAnalytics.Core.RiskEngine;
using System.Security.Claims;

namespace RiskAnalytics.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IRiskEngine _riskEngine;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(IRiskEngine riskEngine, IRedisCacheService redisCacheService, ILogger<ExportController> logger)
        {
            _riskEngine = riskEngine;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        [HttpGet("portfolio/{userId}/csv")]
        public async Task<IActionResult> ExportPortfolioCsv(int userId)
        {
            // Get current user from JWT token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId) || currentUserId != userId)
            {
                return Forbid("You can only export your own portfolios");
            }

            try
            {
                var memoryStream = new MemoryStream();
                await _riskEngine.StreamPortfolioCsvAsync(userId, memoryStream);

                memoryStream.Position = 0; // Reset stream position for reading

                return File(memoryStream, "text/csv", $"portfolio_{userId}_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export portfolio CSV for user {UserId}", userId);
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
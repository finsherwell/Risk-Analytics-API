using Microsoft.AspNetCore.Mvc;
using RiskAnalytics.Api.ResponseModels;
using RiskAnalytics.Core.Cache;
using RiskAnalytics.Core.RiskEngine;

namespace RiskAnalytics.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IRiskEngine _riskEngine;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<StatusController> _logger;

        public StatusController(IRiskEngine riskEngine, IRedisCacheService redisCacheService, ILogger<StatusController> logger)
        {
            _riskEngine = riskEngine;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetApiStatus()
        {
            var status = await _riskEngine.GetApiStatus();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "API status retrieved successfully",
                Data = new
                {
                    status = status.IsHealthy ? "Healthy" : "Unhealthy",
                    uptime = status.Uptime,
                    version = status.Version,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    timestamp = DateTime.UtcNow
                }
            });
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "API is running", timestamp = DateTime.UtcNow });
        }
    }
}
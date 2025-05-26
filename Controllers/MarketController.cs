using Microsoft.AspNetCore.Mvc;
using RiskAnalytics.Api.ResponseModels;
using RiskAnalytics.Core.Cache;
using RiskAnalytics.Core.Models;
using RiskAnalytics.Core.RiskEngine;

namespace RiskAnalytics.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketController : ControllerBase
    {
        private readonly IRiskEngine _riskEngine;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<MarketController> _logger;

        public MarketController(IRiskEngine riskEngine, IRedisCacheService redisCacheService, ILogger<MarketController> logger)
        {
            _riskEngine = riskEngine;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        [HttpGet("current-rate")]
        public async Task<ActionResult<ApiResponse>> GetCurrentRate()
        {
            string cacheKey = "current-interest-rate";
            var cachedRate = await _redisCacheService.GetAsync<decimal?>(cacheKey);

            if (cachedRate.HasValue)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Current rate retrieved from cache",
                    Data = new { rate = cachedRate.Value, source = "cache" }
                });
            }

            var currentRate = await _riskEngine.GetCurrentInterestRateAsync();
            await _redisCacheService.SetAsync(cacheKey, currentRate.Rate, TimeSpan.FromMinutes(5));

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Current rate retrieved successfully",
                Data = new { rate = currentRate.Rate, lastUpdated = currentRate.LastUpdated }
            });
        }

        [HttpGet("yield-curve")]
        public async Task<ActionResult<ApiResponse>> GetYieldCurve([FromQuery] string bondName = "US Treasury")
        {
            // Create a sample bond for yield curve calculation
            var bond = new Bond { BondName = bondName };
            var yieldCurve = await _riskEngine.GetYieldCurveAsync(bond);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Yield curve retrieved successfully",
                Data = new
                {
                    bondName = yieldCurve.BondName,
                    points = yieldCurve.YieldPoints,
                    asOfDate = yieldCurve.AsOfDate
                }
            });
        }

        [HttpGet("last-update")]
        public ActionResult<ApiResponse> GetLastUpdate()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Last update time retrieved",
                Data = new { lastUpdate = DateTime.UtcNow }
            });
        }
    }
}
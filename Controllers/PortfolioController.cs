using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskAnalytics.Api.RequestModels;
using RiskAnalytics.Api.ResponseModels;
using RiskAnalytics.Core.Cache;
using RiskAnalytics.Core.RiskEngine;
using System.Security.Claims;

namespace RiskAnalytics.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all portfolio operations
    public class PortfolioController : ControllerBase
    {
        private readonly IRiskEngine _riskEngine;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<PortfolioController> _logger;

        public PortfolioController(IRiskEngine riskEngine, IRedisCacheService redisCacheService, ILogger<PortfolioController> logger)
        {
            _riskEngine = riskEngine;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        // Helper method to get user ID from JWT token
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<PortfolioDto>>> GetUserPortfolio(int userId, [FromQuery] string portfolioName = "Default")
        {
            // Check if user can access this portfolio
            int currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
                return Forbid("You can only access your own portfolios");

            var portfolio = await _riskEngine.GetUserPortfolio(userId, portfolioName);

            // Convert to DTO (Data Transfer Object)
            var portfolioDto = new PortfolioDto
            {
                PortfolioId = portfolio.PortfolioId,
                PortfolioName = portfolio.PortfolioName,
                TotalValue = portfolio.TotalValue,
                AvgYield = portfolio.AvgYield,
                AvgDuration = portfolio.AvgDuration,
                TotalDV01 = portfolio.TotalDV01,
                Bonds = portfolio.Bonds.Select(b => new BondDto
                {
                    BondId = b.BondId,
                    BondName = b.BondName,
                    Coupon = b.Coupon,
                    Maturity = b.Maturity,
                    Price = b.Price,
                    Quantity = b.Quantity
                }).ToList()
            };

            return Ok(new ApiResponse<PortfolioDto>
            {
                Success = true,
                Message = "Portfolio retrieved successfully",
                Data = portfolioDto
            });
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreatePortfolio([FromBody] CreatePortfolioRequest request)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId != request.UserId)
                return Forbid("You can only create portfolios for yourself");

            bool success = await _riskEngine.CreatePortfolio(request.UserId);

            if (success)
            {
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Portfolio created successfully"
                });
            }

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Failed to create portfolio"
            });
        }

        [HttpPost("{portfolioId}/bonds")]
        public async Task<ActionResult<ApiResponse>> AddBondToPortfolio(int portfolioId, [FromBody] AddBondRequest bondRequest)
        {
            // In a real implementation, you'd pass the bond details to the risk engine
            bool success = await _riskEngine.AddBondToPortfolio(portfolioId /*, bond details would go here */);

            if (success)
            {
                // Clear cache for this portfolio
                await _redisCacheService.DeleteAsync($"portfolio:{portfolioId}");

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Bond added to portfolio successfully"
                });
            }

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Failed to add bond to portfolio"
            });
        }

        [HttpDelete("{portfolioId}/bonds/{bondId}")]
        public async Task<ActionResult<ApiResponse>> RemoveBondFromPortfolio(int portfolioId, int bondId)
        {
            bool success = await _riskEngine.RemoveBondFromPortfolio(portfolioId, bondId);

            if (success)
            {
                await _redisCacheService.DeleteAsync($"portfolio:{portfolioId}");
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Bond removed from portfolio successfully"
                });
            }

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Failed to remove bond from portfolio"
            });
        }

        [HttpPost("{portfolioId}/stress-test")]
        public async Task<ActionResult<ApiResponse<StressTestDto>>> RunStressTest(int portfolioId, [FromBody] ShockScenarioRequest shockRequest)
        {
            var result = await _riskEngine.CalculateStressAsync(portfolioId, shockRequest.ShockBps, shockRequest.ShockDirection);

            var stressTestDto = new StressTestDto
            {
                ShockBps = shockRequest.ShockBps,
                ShockDirection = shockRequest.ShockDirection,
                OriginalValue = result.OriginalValue,
                StressedValue = result.StressedValue,
                PnL = result.PnL,
                PnLPercent = result.PnLPercent,
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(new ApiResponse<StressTestDto>
            {
                Success = true,
                Message = "Stress test completed successfully",
                Data = stressTestDto
            });
        }

        [HttpPost("{portfolioId}/risk-analysis")]
        public async Task<ActionResult<ApiResponse<RiskAnalysisDto>>> CalculateRisk(int portfolioId)
        {
            var result = await _riskEngine.CalculateRiskAsync(portfolioId);

            var riskDto = new RiskAnalysisDto
            {
                VaR95 = result.VaR95,
                VaR99 = result.VaR99,
                Duration = result.Duration,
                Convexity = result.Convexity,
                DV01 = result.DV01,
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(new ApiResponse<RiskAnalysisDto>
            {
                Success = true,
                Message = "Risk analysis completed successfully",
                Data = riskDto
            });
        }
    }
}
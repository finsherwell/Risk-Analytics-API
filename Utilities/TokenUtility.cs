using Microsoft.IdentityModel.Tokens;
using RiskAnalytics.Api.Secrets;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RiskAnalytics.Api.Utilities
{
    public class TokenUtility
    {
        public static string GenerateJwtToken(int userId)
        {
            string? jwtToken = SecretsManager.GetJwtToken();

            if (jwtToken == null)
            {
                throw new InvalidOperationException("JWT secret is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtToken));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "User")
            };

            var token = new JwtSecurityToken(
                issuer: "RiskAnalytics",
                audience: "RiskAnalyticsUser",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

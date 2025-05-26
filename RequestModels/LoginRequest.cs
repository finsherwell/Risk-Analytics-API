namespace RiskAnalytics.Api.RequestModels
{
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? NewUsername { get; set; }
        public string? NewPassword { get; set; }
    }
}

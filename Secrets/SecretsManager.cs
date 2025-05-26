namespace RiskAnalytics.Api.Secrets
{
    public class SecretsManager
    {
        private static readonly IConfiguration _config;
        static SecretsManager()
        {
            _config = new ConfigurationBuilder()
            .AddUserSecrets<SecretsManager>()
            .Build();
        }
        public static string? GetJwtToken()
        {
            return _config["JwtToken"];
        }
    }
}

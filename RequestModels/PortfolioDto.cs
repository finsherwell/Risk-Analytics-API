namespace RiskAnalytics.Api.RequestModels
{
    public class PortfolioDto
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal AvgYield { get; set; }
        public decimal AvgDuration { get; set; }
        public decimal TotalDV01 { get; set; }
        public List<BondDto> Bonds { get; set; } = new();
    }

    public class BondDto
    {
        public int BondId { get; set; }
        public string BondName { get; set; } = string.Empty;
        public decimal Coupon { get; set; }
        public DateTime Maturity { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}

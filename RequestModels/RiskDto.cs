namespace RiskAnalytics.Api.RequestModels
{
    public class RiskAnalysisDto
    {
        public decimal VaR95 { get; set; }
        public decimal VaR99 { get; set; }
        public decimal Duration { get; set; }
        public decimal Convexity { get; set; }
        public decimal DV01 { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class StressTestDto
    {
        public int ShockBps { get; set; }
        public string ShockDirection { get; set; } = string.Empty;
        public decimal OriginalValue { get; set; }
        public decimal StressedValue { get; set; }
        public decimal PnL { get; set; }
        public decimal PnLPercent { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}

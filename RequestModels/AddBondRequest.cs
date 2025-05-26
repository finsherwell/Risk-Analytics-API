namespace RiskAnalytics.Api.RequestModels
{
    public class AddBondRequest
    {
        public string BondName { get; set; }
        public decimal Coupon { get; set; }
        public DateTime Maturity { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
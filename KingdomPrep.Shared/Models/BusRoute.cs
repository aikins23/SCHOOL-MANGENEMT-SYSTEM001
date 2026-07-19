namespace KingdomPrep.Shared.Models
{
    /// <summary>A transport route with a fee and payment term, optionally served by a bus.</summary>
    public class BusRoute
    {
        public int RouteId { get; set; }
        public string RouteName { get; set; } = "";
        public decimal Fee { get; set; }
        public string PaymentTerm { get; set; } = "Monthly"; // 'Daily' | 'Weekly' | 'Monthly'
        public int? BusId { get; set; }
        public string BusLabel { get; set; } = "";           // joined for display
        public string Stops { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}

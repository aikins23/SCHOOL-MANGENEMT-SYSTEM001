using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A bus student's transport standing for the current period.</summary>
    public class TransportArrear
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = "";
        public string RouteName { get; set; } = "";
        public string Term { get; set; } = "";
        public string Period { get; set; } = "";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Fee { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance => Math.Max(0m, Fee - Paid);
        public int PresentDays { get; set; }
        public string GuardianPhone { get; set; } = "";
    }
}

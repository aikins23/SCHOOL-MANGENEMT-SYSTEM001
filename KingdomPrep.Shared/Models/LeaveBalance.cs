using System;

namespace KingdomPrep.Shared.Models
{
    public class LeaveBalance
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string TermName { get; set; }
        public DateTime TermStart { get; set; }
        public DateTime TermEnd { get; set; }
        public int Entitlement { get; set; }
        public int DaysUsed { get; set; }
        public int Remaining => Math.Max(0, Entitlement - DaysUsed);
    }
}

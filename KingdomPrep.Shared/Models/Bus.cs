using System;

namespace KingdomPrep.Shared.Models
{
    /// <summary>A school bus/vehicle.</summary>
    public class Bus
    {
        public int BusId { get; set; }
        public string Label { get; set; } = "";
        public string RegNumber { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string DriverContact { get; set; } = "";
        public int Capacity { get; set; }
        public string Notes { get; set; } = "";
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public override string ToString() => Label; // ComboBox display
    }
}

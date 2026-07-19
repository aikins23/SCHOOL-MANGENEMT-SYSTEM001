using System;

namespace KingdomPrep.Shared.Models
{
    /// <summary>
    /// Represents a school class configuration
    /// </summary>
    public class ClassConfig
    {
        public string ClassName { get; set; }
        public decimal TuitionFee { get; set; }
        public int PromotionLevel { get; set; }
    }
}

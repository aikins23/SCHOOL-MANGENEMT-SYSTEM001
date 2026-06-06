namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// One grade band: scores &gt;= MinScore (and below the next higher band's MinScore) get this
    /// Code (printed in the subject Grade column) and Label (remark + legend text).
    /// </summary>
    public class GradeBand
    {
        public int MinScore { get; set; }
        public string Code { get; set; } = "";
        public string Label { get; set; } = "";
    }
}

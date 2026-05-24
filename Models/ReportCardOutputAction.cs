namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Specifies how to output the report card (print or save)
    /// </summary>
    public enum OutputType
    {
        Print,
        Save
    }

    public class ReportCardOutputAction
    {
        public OutputType Type { get; set; }
        public string PrinterName { get; set; }      // For Print action
        public string SavePath { get; set; }         // For Save action
    }

    /// <summary>
    /// Progress report for batch report card generation
    /// </summary>
    public class BatchProgressReport
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public int Percentage => Total == 0 ? 0 : (Current * 100) / Total;
    }
}

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a subject's exam results and ranking for a student
    /// </summary>
    public class SubjectResult
    {
        public string Subject { get; set; }
        public decimal ClassScore { get; set; }        // 0-60 (Test + Group + Project)
        public decimal ExamScore { get; set; }         // 0-100
        public decimal TotalScore { get; set; }        // Calculated
        public string Grade { get; set; }              // "1", "2", "3", "4", "5"
        public string Remark { get; set; }             // "Advanced", "Proficiency", etc.
        public int PositionInClass { get; set; }       // Rank: 1st, 2nd, 3rd, etc.
    }
}

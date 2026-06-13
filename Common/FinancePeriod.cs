using System;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public enum FinanceWindow { Term, Year, AllTime, Custom }

    /// <summary>Resolves a reporting window to an inclusive [From,To] date range + a label.</summary>
    public static class FinancePeriod
    {
        public static (DateTime From, DateTime To, string Label) Resolve(
            FinanceWindow window, DateTime today, DateTime? customFrom = null, DateTime? customTo = null)
        {
            switch (window)
            {
                case FinanceWindow.Year:
                    return (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31), today.Year.ToString());
                case FinanceWindow.AllTime:
                    return (new DateTime(2000, 1, 1), new DateTime(2100, 12, 31), "All time");
                case FinanceWindow.Custom:
                {
                    DateTime f = (customFrom ?? today).Date, t = (customTo ?? today).Date;
                    if (f > t) { var tmp = f; f = t; t = tmp; }
                    return (f, t, f.ToString("dd MMM yyyy") + " - " + t.ToString("dd MMM yyyy"));
                }
                default:
                    var term = AppConfig.Leave.GetTerm(today);
                    return (term.Start, term.End, term.TermName);
            }
        }
    }
}

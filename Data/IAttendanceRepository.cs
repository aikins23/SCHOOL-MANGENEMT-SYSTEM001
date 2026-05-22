using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IAttendanceRepository
    {
        Task EnsureTableExistsAsync();
        Task<DataTable> GetTargetListAsync(string type, string classId, DateTime date);
        Task<DataTable> GetMonthlyAnalysisAsync(string type, int month, int year);
        Task<bool> SaveAttendanceBatchAsync(IEnumerable<Models.AttendanceRecord> records);
        Task<IEnumerable<string>> GetActiveClassesAsync();
    }
}

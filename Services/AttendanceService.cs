using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class AttendanceService
    {
        private readonly IAttendanceRepository _repository;

        public AttendanceService(IAttendanceRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task InitializeAsync()
        {
            await _repository.EnsureTableExistsAsync();
        }

        public async Task<DataTable> GetAttendanceListAsync(string type, string classId, DateTime date)
        {
            return await _repository.GetTargetListAsync(type, classId, date);
        }

        public async Task<DataTable> GetMonthlyAnalysisAsync(string type, int month, int year)
        {
            return await _repository.GetMonthlyAnalysisAsync(type, month, year);
        }

        public async Task<(bool Success, string Message)> SaveBatchAsync(IEnumerable<Models.AttendanceRecord> records)
        {
            try
            {
                bool success = await _repository.SaveAttendanceBatchAsync(records);
                return success 
                    ? (true, "Attendance records saved successfully.") 
                    : (false, "Failed to save attendance records.");
            }
            catch (Exception ex)
            {
                return (false, "Error saving attendance: " + ex.Message);
            }
        }

        public async Task<IEnumerable<string>> GetClassesAsync()
        {
            return await _repository.GetActiveClassesAsync();
        }
    }
}

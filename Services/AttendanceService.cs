using KingdomPrep.Shared.Models;
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
        private readonly IStudentRepository _studentRepo;

        public AttendanceService(IAttendanceRepository repository, IStudentRepository studentRepo)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _studentRepo = studentRepo ?? throw new ArgumentNullException(nameof(studentRepo));
        }

        public async Task<DataTable> GetAttendanceListAsync(string type, string classId, DateTime date)
        {
            return await _repository.GetTargetListAsync(type, classId, date);
        }

        public async Task<DataTable> GetMonthlyAnalysisAsync(string type, int month, int year)
        {
            return await _repository.GetMonthlyAnalysisAsync(type, month, year);
        }

        public async Task<(bool Success, string Message)> SaveBatchAsync(IEnumerable<KingdomPrep.Shared.Models.AttendanceRecord> records)
        {
            try
            {
                bool success = await _repository.SaveAttendanceBatchAsync(records);
                if (success)
                {
                    // Trigger asynchronous background notifications for absent students
                    _ = ProcessAbsenteeNotificationsAsync(records);
                    return (true, "Attendance records saved successfully.");
                }
                return (false, "Failed to save attendance records.");
            }
            catch (Exception ex)
            {
                return (false, "Error saving attendance: " + ex.Message);
            }
        }

        private async Task ProcessAbsenteeNotificationsAsync(IEnumerable<KingdomPrep.Shared.Models.AttendanceRecord> records)
        {
            foreach (var record in records)
            {
                if (record.ReferenceType == "STUDENT" && record.Status.ToUpperInvariant() == "ABSENT")
                {
                    try
                    {
                        var student = await _studentRepo.GetByIdAsync(record.ReferenceID);
                        if (student != null)
                        {
                            // 1. Send SMS (durable)
                            if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                            {
                                await SmsService.SendAttendanceAlertAsync(
                                    student.EmergencyContact,
                                    student.FullName,
                                    record.Status,
                                    record.Date);
                            }

                            // 2. Send Email
                            if (!string.IsNullOrWhiteSpace(student.GuardianEmail))
                            {
                                await NotificationService.SendAttendanceAlertAsync(
                                    student.FullName,
                                    student.GuardianEmail,
                                    record.Status,
                                    record.Date);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogWarning($"Failed to notify parent for student {record.ReferenceID}: {ex.Message}");
                    }
                }
            }
        }

        public async Task<IEnumerable<string>> GetClassesAsync()
        {
            return await _repository.GetActiveClassesAsync();
        }
    }
}

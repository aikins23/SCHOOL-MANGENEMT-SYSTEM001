using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class TimetableGeneratorService
    {
        private readonly Data.TimetableRepository _repository;

        public TimetableGeneratorService(Data.TimetableRepository repository)
        {
            _repository = repository;
        }

        public async Task<(bool Success, string Message, List<TimetableEntry> Entries)> GenerateForClassAsync(string classId)
        {
            try
            {
                var periods = (await _repository.GetPeriodsAsync()).Where(p => !p.IsBreak).OrderBy(p => p.SortOrder).ToList();
                var allocations = await _repository.GetAllocationsAsync(classId);
                var allAllocations = await _repository.GetAllocationsAsync(); // Needed to check teacher conflicts across other classes

                if (!allocations.Any()) return (false, "No subject allocations defined for this class.", null);
                if (!periods.Any()) return (false, "No teaching periods defined in the system.", null);

                // Total slots available: Days (5) * Periods
                int totalAvailableSlots = 5 * periods.Count;
                int totalRequiredSlots = allocations.Sum(a => a.PeriodsPerWeek);

                if (totalRequiredSlots > totalAvailableSlots)
                    return (false, $"Insufficient slots. Required: {totalRequiredSlots}, Available: {totalAvailableSlots}.", null);

                var generatedEntries = new List<TimetableEntry>();
                var random = new Random();

                // Simple Greedy Algorithm with Teacher Conflict Check
                // 1. Prepare the grid (Day, Period)
                var days = Enumerable.Range(1, 5).ToList();
                var availableSlots = new List<(int Day, int PeriodId)>();
                foreach (var d in days)
                    foreach (var p in periods)
                        availableSlots.Add((d, p.PeriodID));

                // 2. Shuffle slots for variety
                availableSlots = availableSlots.OrderBy(x => random.Next()).ToList();

                // 3. Flatten allocations into a list of single-period tasks
                var tasks = new List<SubjectAllocation>();
                foreach (var alloc in allocations)
                    for (int i = 0; i < alloc.PeriodsPerWeek; i++)
                        tasks.Add(alloc);

                // 4. Attempt to place tasks into slots
                foreach (var task in tasks)
                {
                    bool placed = false;
                    for (int i = 0; i < availableSlots.Count; i++)
                    {
                        var slot = availableSlots[i];

                        // Check if teacher is busy in another class during this exact slot
                        bool teacherBusy = await IsTeacherBusyAsync(task.TeacherID, slot.Day, slot.PeriodId, classId);
                        
                        if (!teacherBusy)
                        {
                            generatedEntries.Add(new TimetableEntry
                            {
                                ClassID = classId,
                                DayOfWeek = slot.Day,
                                PeriodID = slot.PeriodId,
                                SubjectName = task.SubjectName,
                                TeacherID = task.TeacherID
                            });
                            availableSlots.RemoveAt(i);
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        return (false, $"Could not place {task.SubjectName} due to teacher schedule conflicts. Try reducing teacher workload or increasing periods.", null);
                    }
                }

                return (true, "Timetable generated successfully.", generatedEntries);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Timetable generation failed", ex);
                return (false, "Generation error: " + ex.Message, null);
            }
        }

        private async Task<bool> IsTeacherBusyAsync(int? teacherId, int day, int periodId, string currentClassId)
        {
            if (teacherId == null) return false;

            // This is a simplified check. In a production system, we'd load all existing entries 
            // across all classes into memory once to avoid multiple DB hits during generation.
            // For this project, we'll assume a reasonably sized dataset.
            
            using (var conn = new System.Data.OleDb.OleDbConnection(Common.AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                var query = "SELECT COUNT(*) FROM TimetableEntries WHERE TeacherID = ? AND DayOfWeek = ? AND PeriodID = ? AND ClassID <> ?";
                using (var cmd = new System.Data.OleDb.OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", teacherId);
                    cmd.Parameters.AddWithValue("?", day);
                    cmd.Parameters.AddWithValue("?", periodId);
                    cmd.Parameters.AddWithValue("?", currentClassId);
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
            }
        }
    }
}

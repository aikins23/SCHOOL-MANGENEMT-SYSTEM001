using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;

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
            var allocations = await _repository.GetAllocationsAsync(classId);
            return await GenerateForClassAsync(classId, allocations);
        }

        public async Task<(bool Success, string Message, List<TimetableEntry> Entries)> GenerateForClassAsync(string classId, List<SubjectAllocation> allocations)
        {
            try
            {
                var periods = (await _repository.GetPeriodsAsync()).Where(p => !p.IsBreak).OrderBy(p => p.SortOrder).ToList();
                allocations = allocations ?? new List<SubjectAllocation>();
                var teacherBusySlots = await _repository.GetTeacherBusySlotsExcludingClassesAsync(new[] { classId });
                var result = GenerateClassEntries(classId, allocations, periods, teacherBusySlots);
                return result.Success
                    ? ValidateEntries(result.Entries, enforceTeacherConflicts: true)
                    : result;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Timetable generation failed", ex);
                return (false, "Generation error: " + ex.Message, null);
            }
        }

        public async Task<(bool Success, string Message, Dictionary<string, List<TimetableEntry>> Batches)> GenerateForDepartmentAsync(string departmentName, Dictionary<string, List<SubjectAllocation>> allocationsByClass)
        {
            return await GenerateForDepartmentAsync(departmentName, allocationsByClass, enforceTeacherConflicts: true);
        }

        public async Task<(bool Success, string Message, Dictionary<string, List<TimetableEntry>> Batches)> GenerateForDepartmentAsync(string departmentName, Dictionary<string, List<SubjectAllocation>> allocationsByClass, bool enforceTeacherConflicts)
        {
            try
            {
                allocationsByClass = allocationsByClass ?? new Dictionary<string, List<SubjectAllocation>>(StringComparer.OrdinalIgnoreCase);
                var classIds = allocationsByClass.Keys.ToList();
                if (classIds.Count == 0)
                {
                    return (false,
                        "No classes or workload requirements were found for " + (departmentName ?? "the selected department") + ".",
                        null);
                }

                var periods = (await _repository.GetPeriodsAsync()).Where(p => !p.IsBreak).OrderBy(p => p.SortOrder).ToList();
                var teacherBusySlots = enforceTeacherConflicts
                    ? await _repository.GetTeacherBusySlotsExcludingClassesAsync(classIds)
                    : new HashSet<string>();
                var batches = new Dictionary<string, List<TimetableEntry>>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in allocationsByClass)
                {
                    var result = GenerateClassEntries(item.Key, item.Value, periods, teacherBusySlots);
                    if (!result.Success)
                        return (false, result.Message, null);

                    batches[item.Key] = result.Entries;
                    if (enforceTeacherConflicts)
                    {
                        foreach (var entry in result.Entries)
                        {
                            if (entry.TeacherID.HasValue)
                            {
                                teacherBusySlots.Add(TimetableRepository.BuildTeacherSlotKey(entry.TeacherID.Value, entry.DayOfWeek, entry.PeriodID));
                            }
                        }
                    }
                }

                var allEntries = batches.Values.SelectMany(x => x).ToList();
                var validation = ValidateEntries(allEntries, enforceTeacherConflicts);
                return validation.Success
                    ? (true, "Department timetable generated successfully.", batches)
                    : (false, validation.Message, null);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Department timetable generation failed", ex);
                return (false, "Generation error: " + ex.Message, null);
            }
        }

        private (bool Success, string Message, List<TimetableEntry> Entries) GenerateClassEntries(string classId, List<SubjectAllocation> allocations, List<TimePeriod> periods, HashSet<string> teacherBusySlots)
        {
            allocations = allocations ?? new List<SubjectAllocation>();
            if (!allocations.Any()) return (false, "No subject allocations defined for " + classId + ".", null);
            if (!periods.Any())
            {
                return (false,
                    "No teachable periods are available for timetable generation.\n\n" +
                    "Go to Period Setup and save at least one non-break teaching period. " +
                    "Silence Hour, Assembly, Break, Lunch, and Closing are fixed periods and are not used for subject lessons.",
                    null);
            }

            int totalAvailableSlots = 5 * periods.Count;
            int totalRequiredSlots = allocations.Sum(a => a.PeriodsPerWeek);
            if (totalRequiredSlots > totalAvailableSlots)
            {
                var subjectCount = allocations.Count;
                var averagePeriods = subjectCount == 0 ? 0 : (decimal)totalRequiredSlots / subjectCount;
                var message =
                    "The timetable cannot be generated because the selected workload needs more lesson slots than the saved period setup provides.\n\n" +
                    "Class: " + classId + "\n" +
                    $"Required periods: {totalRequiredSlots}\n" +
                    $"Available periods: {totalAvailableSlots}\n\n" +
                    $"You selected {subjectCount} subject(s), averaging about {averagePeriods:0.#} period(s) each. " +
                    "Reduce the required periods or add more non-break teaching periods in Period Setup.";
                return (false, message, null);
            }

            var generatedEntries = new List<TimetableEntry>();
            var random = new Random();
            var availableSlots = new List<Tuple<int, int>>();
            foreach (var day in Enumerable.Range(1, 5))
                foreach (var period in periods)
                    availableSlots.Add(Tuple.Create(day, period.PeriodID));

            availableSlots = availableSlots.OrderBy(x => random.Next()).ToList();
            var tasks = new List<SubjectAllocation>();
            foreach (var alloc in allocations)
                for (int i = 0; i < alloc.PeriodsPerWeek; i++)
                    tasks.Add(alloc);

            foreach (var task in tasks)
            {
                bool placed = false;
                for (int i = 0; i < availableSlots.Count; i++)
                {
                    var slot = availableSlots[i];
                    bool teacherBusy = task.TeacherID.HasValue
                        && teacherBusySlots.Contains(TimetableRepository.BuildTeacherSlotKey(task.TeacherID.Value, slot.Item1, slot.Item2));

                    if (!teacherBusy)
                    {
                        generatedEntries.Add(new TimetableEntry
                        {
                            ClassID = classId,
                            DayOfWeek = slot.Item1,
                            PeriodID = slot.Item2,
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
                    return (false,
                        "The timetable cannot place all lessons without a teacher clash.\n\n" +
                        "Teacher: " + (task.TeacherName ?? "Selected teacher") + "\n" +
                        "Class: " + classId + "\n" +
                        "Subject: " + task.SubjectName + "\n\n" +
                        "Assign another teacher, reduce the workload, or add more non-break teaching periods.",
                        null);
                }
            }

            return (true, "Timetable generated successfully.", generatedEntries);
        }

        private (bool Success, string Message, List<TimetableEntry> Entries) ValidateEntries(List<TimetableEntry> entries, bool enforceTeacherConflicts)
        {
            if (enforceTeacherConflicts)
            {
                var teacherConflict = entries
                    .Where(e => e.TeacherID.HasValue)
                    .GroupBy(e => new { e.TeacherID, e.DayOfWeek, e.PeriodID })
                    .FirstOrDefault(g => g.Count() > 1);
                if (teacherConflict != null)
                {
                    var rows = string.Join(", ", teacherConflict.Select(e => e.ClassID + " " + e.SubjectName));
                    return (false,
                        "The generated timetable has a teacher conflict.\n\n" +
                        "One teacher is assigned to more than one class at the same time: " + rows + ".",
                        null);
                }
            }

            var classConflict = entries
                .GroupBy(e => new { e.ClassID, e.DayOfWeek, e.PeriodID })
                .FirstOrDefault(g => g.Count() > 1);
            if (classConflict != null)
            {
                return (false,
                    "The generated timetable has a class conflict for " + classConflict.Key.ClassID + ".\n\n" +
                    "The class has more than one subject in the same period.",
                    null);
            }

            return (true, "Timetable validation passed.", entries);
        }

    }
}

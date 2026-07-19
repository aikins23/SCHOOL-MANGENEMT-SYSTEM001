using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class AcademicSessionService
    {
        private readonly AcademicSessionRepository _repository;
        private readonly AcademicSessionReportService _reportService;

        public AcademicSessionService()
            : this(new AcademicSessionRepository(Common.AppConfig.ConnectionString), new AcademicSessionReportService())
        {
        }

        public AcademicSessionService(AcademicSessionRepository repository, AcademicSessionReportService reportService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        public async Task InitializeAsync()
        {
            await _repository.EnsureSchemaAsync();
        }

        public Task<List<AcademicYear>> GetYearsAsync() => _repository.GetYearsAsync();

        public Task<List<AcademicTerm>> GetTermsAsync(bool includeClosed = true) => _repository.GetTermsAsync(includeClosed);

        public Task<AcademicTerm> GetActiveTermAsync() => _repository.GetActiveTermAsync();

        public Task<AcademicTerm> FindTermAsync(string termName, string academicYearName = null)
            => _repository.FindTermAsync(termName, academicYearName);

        public async Task<(bool Success, string Message, int YearId)> CreateAcademicYearAsync(AcademicYear year)
        {
            if (year == null) return (false, "Academic year is required.", 0);
            if (string.IsNullOrWhiteSpace(year.YearName)) return (false, "Enter the academic year name.", 0);
            if (year.EndDate.Date <= year.StartDate.Date) return (false, "Academic year end date must be after the start date.", 0);

            try
            {
                var id = await _repository.CreateAcademicYearAsync(year);
                return (true, "Academic year created.", id);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Create academic year failed", ex);
                return (false, "Could not create academic year: " + ex.Message, 0);
            }
        }

        public async Task<(bool Success, string Message, int TermId)> CreateTermAsync(AcademicTerm term)
        {
            if (term == null) return (false, "Academic term is required.", 0);
            if (term.AcademicYearID <= 0) return (false, "Select an academic year.", 0);
            if (string.IsNullOrWhiteSpace(term.TermName)) return (false, "Enter the term name.", 0);
            if (term.EndDate.Date <= term.StartDate.Date) return (false, "Term end date must be after the start date.", 0);
            if (term.ReopeningDate.HasValue && term.ReopeningDate.Value.Date <= term.EndDate.Date)
                return (false, "Reopening date must be after the term closing date.", 0);

            try
            {
                var id = await _repository.CreateTermAsync(term);
                await _repository.ScheduleDefaultRemindersAsync(id, term.StartDate);
                await _repository.CarryForwardLatestClosedDebtToTermAsync(id);
                return (true, "Academic term created.", id);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Create academic term failed", ex);
                return (false, "Could not create term: " + ex.Message, 0);
            }
        }

        public async Task<(bool Success, string Message)> SetActiveTermAsync(int termId)
        {
            if (termId <= 0) return (false, "Select a term first.");

            try
            {
                await _repository.SetActiveTermAsync(termId);
                await _repository.CarryForwardLatestClosedDebtToTermAsync(termId);
                return (true, "Active term updated and fee ledgers prepared.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Set active term failed", ex);
                return (false, "Could not set active term: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message, string ReportPath)> CloseTermAsync(int termId)
        {
            if (termId <= 0) return (false, "Select a term first.", "");

            try
            {
                var term = await _repository.GetTermByIdAsync(termId);
                if (term == null) return (false, "Selected term was not found.", "");
                if (term.IsClosed) return (false, "This term is already closed.", term.ClosureReportPath ?? "");

                await _repository.EnsureLedgersForTermAsync(termId);
                var summary = await _repository.GetClosureSummaryAsync(termId);
                var reportPath = await _reportService.GenerateClosureReportAsync(summary);
                await _repository.CloseTermAsync(termId, reportPath);
                var nextTermId = await _repository.FindNextOpenTermAsync(termId);
                if (nextTermId.HasValue)
                {
                    await _repository.CarryForwardDebtAsync(termId, nextTermId.Value);
                    return (true, "Term closed, closure report generated, and unpaid balances carried forward.", reportPath);
                }

                return (true, "Term closed and closure report generated. Create the next term to carry unpaid balances forward.", reportPath);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Close term failed", ex);
                return (false, "Could not close term: " + ex.Message, "");
            }
        }

        public async Task<int> ProcessDueReopeningRemindersAsync()
        {
            try
            {
                return await _repository.ProcessDueReopeningRemindersAsync();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Academic reopening reminders: " + ex.Message);
                return 0;
            }
        }

        public Task<StudentBillingBreakdown> GetStudentBillingBreakdownAsync(string studentId, int? termId = null)
        {
            return _repository.GetStudentBillingBreakdownAsync(studentId, termId);
        }
    }
}

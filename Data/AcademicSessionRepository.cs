using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class AcademicSessionRepository
    {
        private readonly string _connectionString;

        public AcademicSessionRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public Task EnsureSchemaAsync() => AcademicSessionSchema.EnsureAsync(_connectionString);

        public async Task<List<AcademicYear>> GetYearsAsync()
        {
            var years = new List<AcademicYear>();
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
SELECT AcademicYearID, YearName, StartDate, EndDate, IsActive
FROM AcademicYears
WHERE SchoolId = @p0
ORDER BY StartDate DESC, AcademicYearID DESC", c))
                {
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync()) years.Add(MapYear(r));
                    }
                }
            }
            return years;
        }

        public async Task<List<AcademicTerm>> GetTermsAsync(bool includeClosed = true)
        {
            var terms = new List<AcademicTerm>();
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var sql = @"
SELECT t.TermID, t.AcademicYearID, y.YearName, t.TermName, t.StartDate, t.EndDate, t.ReopeningDate,
       t.IsActive, t.IsClosed, t.ClosedAt, t.ClosureReportPath
FROM AcademicTerms t
LEFT JOIN AcademicYears y ON y.AcademicYearID = t.AcademicYearID
WHERE t.SchoolId = @p0";
                if (!includeClosed) sql += " AND t.IsClosed = 0";
                sql += " ORDER BY t.StartDate DESC, t.TermID DESC";

                using (var cmd = new SqlCommand(sql, c))
                {
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync()) terms.Add(MapTerm(r));
                    }
                }
            }
            return terms;
        }

        public async Task<AcademicTerm> GetActiveTermAsync()
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 t.TermID, t.AcademicYearID, y.YearName, t.TermName, t.StartDate, t.EndDate, t.ReopeningDate,
       t.IsActive, t.IsClosed, t.ClosedAt, t.ClosureReportPath
FROM AcademicTerms t
LEFT JOIN AcademicYears y ON y.AcademicYearID = t.AcademicYearID
WHERE t.IsActive = 1 AND t.IsClosed = 0 AND t.SchoolId = @p0
ORDER BY t.StartDate DESC, t.TermID DESC", c))
                {
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        return await r.ReadAsync() ? MapTerm(r) : null;
                    }
                }
            }
        }

        public async Task<AcademicTerm> FindTermAsync(string termName, string academicYearName = null)
        {
            if (string.IsNullOrWhiteSpace(termName)) return null;
            var termNames = BuildTermNameCandidates(termName);
            var termParameters = new List<string>();
            for (int i = 0; i < termNames.Count; i++)
                termParameters.Add("@term" + i);

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var sql = $@"
SELECT TOP 1 t.TermID, t.AcademicYearID, y.YearName, t.TermName, t.StartDate, t.EndDate, t.ReopeningDate,
       t.IsActive, t.IsClosed, t.ClosedAt, t.ClosureReportPath
FROM AcademicTerms t
LEFT JOIN AcademicYears y ON y.AcademicYearID = t.AcademicYearID
WHERE UPPER(LTRIM(RTRIM(t.TermName))) IN ({string.Join(",", termParameters)})";

                if (!string.IsNullOrWhiteSpace(academicYearName))
                    sql += " AND UPPER(LTRIM(RTRIM(ISNULL(y.YearName, '')))) = @yearName";

                sql += " AND t.SchoolId = @SchoolId ORDER BY t.StartDate DESC, t.TermID DESC";

                using (var cmd = new SqlCommand(sql, c))
                {
                    for (int i = 0; i < termNames.Count; i++)
                        cmd.Parameters.AddWithValue(termParameters[i], termNames[i]);
                    if (!string.IsNullOrWhiteSpace(academicYearName))
                        cmd.Parameters.AddWithValue("@yearName", academicYearName.Trim().ToUpperInvariant());
                    TenantContext.AddSchoolParameter(cmd);

                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        return await r.ReadAsync() ? MapTerm(r) : null;
                    }
                }
            }
        }

        public async Task<int> CreateAcademicYearAsync(AcademicYear year)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
INSERT INTO AcademicYears (YearName, StartDate, EndDate, IsActive, SchoolId)
VALUES (@p0, @p1, @p2, 0, @SchoolId)", c))
                {
                    cmd.AddPositionalParameter( year.YearName);
                    cmd.AddPositionalParameter( year.StartDate.Date);
                    cmd.AddPositionalParameter( year.EndDate.Date);
                    TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var id = new SqlCommand("SELECT CAST(@@IDENTITY AS int)", c))
                {
                    var yearId = Convert.ToInt32(await id.ExecuteScalarAsync());
                    await TryRecordSyncUpsertAsync("AcademicYears", "AcademicYearID", yearId, "Insert");
                    return yearId;
                }
            }
        }

        private static List<string> BuildTermNameCandidates(string termName)
        {
            var value = (termName ?? "").Trim().ToUpperInvariant();
            var names = new List<string>();
            void Add(string name)
            {
                if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                    names.Add(name);
            }

            Add(value);
            switch (value)
            {
                case "TERM 1":
                case "1ST TERM":
                case "FIRST TERM":
                    Add("FIRST TERM");
                    Add("TERM 1");
                    Add("1ST TERM");
                    break;
                case "TERM 2":
                case "2ND TERM":
                case "SECOND TERM":
                    Add("SECOND TERM");
                    Add("TERM 2");
                    Add("2ND TERM");
                    break;
                case "TERM 3":
                case "3RD TERM":
                case "THIRD TERM":
                    Add("THIRD TERM");
                    Add("TERM 3");
                    Add("3RD TERM");
                    break;
            }

            return names;
        }

        public async Task CarryForwardLatestClosedDebtToTermAsync(int targetTermId)
        {
            var target = await GetTermByIdAsync(targetTermId);
            if (target == null) return;

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 TermID
FROM AcademicTerms
WHERE IsClosed = 1
  AND EndDate <= @p0
  AND TermID <> @p1
  AND SchoolId = @p2
ORDER BY EndDate DESC, TermID DESC", c))
                {
                    cmd.AddPositionalParameter( target.StartDate.Date);
                    cmd.AddPositionalParameter( targetTermId);
                    TenantContext.AddSchoolParameter(cmd);
                    var raw = await cmd.ExecuteScalarAsync();
                    if (raw == null || raw == DBNull.Value) return;
                    await CarryForwardDebtAsync(Convert.ToInt32(raw), targetTermId);
                }
            }
        }

        public async Task<int> CreateTermAsync(AcademicTerm term)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
INSERT INTO AcademicTerms (AcademicYearID, TermName, StartDate, EndDate, ReopeningDate, IsActive, IsClosed, SchoolId)
VALUES (@p0, @p1, @p2, @p3, @p4, 0, 0, @SchoolId)", c))
                {
                    cmd.AddPositionalParameter( term.AcademicYearID);
                    cmd.AddPositionalParameter( term.TermName);
                    cmd.AddPositionalParameter( term.StartDate.Date);
                    cmd.AddPositionalParameter( term.EndDate.Date);
                    cmd.AddPositionalParameter( term.ReopeningDate.HasValue ? (object)term.ReopeningDate.Value.Date : DBNull.Value);
                    TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var id = new SqlCommand("SELECT CAST(@@IDENTITY AS int)", c))
                {
                    var termId = Convert.ToInt32(await id.ExecuteScalarAsync());
                    await TryRecordSyncUpsertAsync("AcademicTerms", "TermID", termId, "Insert");
                    return termId;
                }
            }
        }

        public async Task SetActiveTermAsync(int termId)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var tx = c.BeginTransaction())
                {
                    try
                    {
                        int yearId;
                        using (var get = new SqlCommand("SELECT AcademicYearID FROM AcademicTerms WHERE TermID = @p0 AND IsClosed = 0 AND SchoolId = @p1", c, tx))
                        {
                            get.AddPositionalParameter( termId);
                            TenantContext.AddSchoolParameter(get);
                            var raw = await get.ExecuteScalarAsync();
                            if (raw == null || raw == DBNull.Value) throw new InvalidOperationException("The selected term was not found or is already closed.");
                            yearId = Convert.ToInt32(raw);
                        }

                        await ExecuteAsync(c, tx, "UPDATE AcademicTerms SET IsActive = 0 WHERE SchoolId = @p0", TenantContext.RequireSchoolId());
                        await ExecuteAsync(c, tx, "UPDATE AcademicYears SET IsActive = 0 WHERE SchoolId = @p0", TenantContext.RequireSchoolId());
                        await ExecuteAsync(c, tx, "UPDATE AcademicTerms SET IsActive = 1 WHERE TermID = @p0 AND SchoolId = @p1", termId, TenantContext.RequireSchoolId());
                        await ExecuteAsync(c, tx, "UPDATE AcademicYears SET IsActive = 1 WHERE AcademicYearID = @p0 AND SchoolId = @p1", yearId, TenantContext.RequireSchoolId());
                        tx.Commit();
                        await TryRecordSyncUpsertAsync("AcademicTerms", "TermID", termId, "Update");
                        await TryRecordSyncUpsertAsync("AcademicYears", "AcademicYearID", yearId, "Update");
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            await EnsureLedgersForTermAsync(termId);
        }

        public async Task CloseTermAsync(int termId, string reportPath)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await ExecuteAsync(c, null, @"
UPDATE AcademicTerms
SET IsClosed = 1, IsActive = 0, ClosedAt = GETDATE(), ClosureReportPath = @p0
WHERE TermID = @p1 AND SchoolId = @p2", reportPath ?? "", termId, TenantContext.RequireSchoolId());
                await TryRecordSyncUpsertAsync("AcademicTerms", "TermID", termId, "Update");
            }
        }

        public async Task<int?> FindNextOpenTermAsync(int closedTermId)
        {
            var closed = await GetTermByIdAsync(closedTermId);
            if (closed == null) return null;

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 TermID
FROM AcademicTerms
WHERE IsClosed = 0
  AND TermID <> @p0
  AND StartDate > @p1
  AND SchoolId = @p2
ORDER BY StartDate ASC, TermID ASC", c))
                {
                    cmd.AddPositionalParameter( closedTermId);
                    cmd.AddPositionalParameter( closed.EndDate.Date);
                    TenantContext.AddSchoolParameter(cmd);
                    var raw = await cmd.ExecuteScalarAsync();
                    return raw == null || raw == DBNull.Value ? (int?)null : Convert.ToInt32(raw);
                }
            }
        }

        public async Task CarryForwardDebtAsync(int sourceTermId, int targetTermId)
        {
            if (sourceTermId <= 0 || targetTermId <= 0 || sourceTermId == targetTermId) return;

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var rows = new List<(string StudentId, string ClassId, decimal Balance)>();
                using (var cmd = new SqlCommand(@"
SELECT l.StudentID, ISNULL(s.ClassID, '') AS ClassID,
       CAST(l.TotalExpectedAmount - l.TotalPaidAmount AS decimal(18,2)) AS Balance
FROM StudentFeeLedger l
LEFT JOIN Students s ON CONVERT(varchar(50), s.StudentID) = l.StudentID AND s.SchoolId = l.SchoolId
WHERE l.TermID = @p0
  AND (l.TotalExpectedAmount - l.TotalPaidAmount) > 0
  AND l.SchoolId = @p1", c))
                {
                    cmd.AddPositionalParameter( sourceTermId);
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                            rows.Add((r["StudentID"].ToString(), r["ClassID"].ToString(), Convert.ToDecimal(r["Balance"])));
                    }
                }

                foreach (var row in rows)
                {
                    var currentCharge = SchoolProfile.FeeForClass(row.ClassId);
                    var insertedLedgerId = await EnsureStudentLedgerAsync(c, targetTermId, row.StudentId, currentCharge);
                    if (insertedLedgerId > 0) await TryRecordSyncUpsertAsync("StudentFeeLedger", "LedgerID", insertedLedgerId, "Insert");

                    using (var exists = new SqlCommand(@"
SELECT CarriedForwardFromTermID
FROM StudentFeeLedger
WHERE TermID = @p0 AND StudentID = @p1 AND SchoolId = @p2", c))
                    {
                        exists.AddPositionalParameter( targetTermId);
                        exists.AddPositionalParameter( row.StudentId);
                        TenantContext.AddSchoolParameter(exists);
                        var carriedFrom = await exists.ExecuteScalarAsync();
                        if (carriedFrom != null && carriedFrom != DBNull.Value && Convert.ToInt32(carriedFrom) == sourceTermId)
                            continue;
                    }

                    await ExecuteAsync(c, null, @"
UPDATE StudentFeeLedger
SET PreviousBalance = PreviousBalance + @p0,
    CurrentTermCharge = @p1,
    TotalExpectedAmount = PreviousBalance + @p2 + @p3,
    CarriedForwardFromTermID = @p4
WHERE TermID = @p5 AND StudentID = @p6 AND SchoolId = @p7",
                        row.Balance,
                        currentCharge,
                        row.Balance,
                        currentCharge,
                        sourceTermId,
                        targetTermId,
                        row.StudentId,
                        TenantContext.RequireSchoolId());
                    var ledgerId = await FindLedgerIdAsync(c, targetTermId, row.StudentId);
                    if (ledgerId > 0) await TryRecordSyncUpsertAsync("StudentFeeLedger", "LedgerID", ledgerId, "Update");
                }
            }
        }

        public async Task ScheduleDefaultRemindersAsync(int termId, DateTime startDate)
        {
            var schoolName = SchoolProfile.DisplayName;
            await InsertReminderIfMissingAsync(termId, "OneWeek", startDate.Date.AddDays(-7),
                $"Reminder: {schoolName} resumes on {startDate:dd MMM yyyy}. Please ensure all outstanding fees are settled.");
            await InsertReminderIfMissingAsync(termId, "OneDay", startDate.Date.AddDays(-1),
                $"School resumes tomorrow, {startDate:dd MMM yyyy}. {schoolName} looks forward to welcoming your ward back to campus.");
        }

        public async Task<int> ProcessDueReopeningRemindersAsync()
        {
            var queued = 0;
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var reminders = new List<(int ReminderId, string Message)>();
                using (var cmd = new SqlCommand(@"
SELECT ReminderID, Message
FROM TermReminderSchedule
WHERE [Status] = 'Pending'
  AND SendOnDate <= CAST(GETDATE() AS date)
  AND SchoolId = @p0
ORDER BY SendOnDate, ReminderID", c))
                {
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                            reminders.Add((Convert.ToInt32(r["ReminderID"]), r["Message"].ToString()));
                    }
                }

                if (reminders.Count == 0) return 0;

                var phones = new List<string>();
                var hasCorrectEmergencyColumn = await HasColumnAsync(c, "Students", "EmergencyContact");
                var hasLegacyEmergencyColumn = await HasColumnAsync(c, "Students", "EmergencyConatct");
                var phoneColumn = hasCorrectEmergencyColumn
                    ? "EmergencyContact"
                    : hasLegacyEmergencyColumn ? "EmergencyConatct" : null;

                if (string.IsNullOrWhiteSpace(phoneColumn))
                {
                    Services.LoggerHelper.LogWarning("Term reminder SMS skipped: Students table has no emergency contact column.");
                }
                else using (var cmd = new SqlCommand($@"
SELECT DISTINCT {phoneColumn} AS Phone
FROM Students
WHERE {phoneColumn} IS NOT NULL
  AND LTRIM(RTRIM({phoneColumn})) <> ''
  AND SchoolId = @p0", c))
                {
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync()) phones.Add(r["Phone"].ToString());
                    }
                }

                foreach (var reminder in reminders)
                {
                    foreach (var phone in phones)
                    {
                        var result = await Services.SmsService.SendSmsAsync(phone, reminder.Message, Services.SmsSenderIds.FeeReminder);
                        if (result.Success) queued++;
                    }

                    await ExecuteAsync(c, null,
                        "UPDATE TermReminderSchedule SET [Status] = 'Queued', SentAt = GETDATE() WHERE ReminderID = @p0",
                        reminder.ReminderId);
                    await TryRecordSyncUpsertAsync("TermReminderSchedule", "ReminderID", reminder.ReminderId, "Update");
                }
            }
            return queued;
        }

        public async Task EnsureLedgersForTermAsync(int termId)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
SELECT CONVERT(varchar(50), StudentID) AS StudentID, ISNULL(ClassID, '') AS ClassID
FROM Students
WHERE SchoolId = @p0", c))
                {
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            var studentId = r["StudentID"].ToString();
                            var classId = r["ClassID"].ToString();
                            var amount = SchoolProfile.FeeForClass(classId);
                            var ledgerId = await EnsureStudentLedgerAsync(c, termId, studentId, amount);
                            if (ledgerId > 0) await TryRecordSyncUpsertAsync("StudentFeeLedger", "LedgerID", ledgerId, "Insert");
                        }
                    }
                }
            }
        }

        public async Task ApplyPaymentToLedgerAsync(int termId, string studentId, string classId, decimal amountPaid)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var insertedLedgerId = await EnsureStudentLedgerAsync(c, termId, studentId, SchoolProfile.FeeForClass(classId));
                if (insertedLedgerId > 0) await TryRecordSyncUpsertAsync("StudentFeeLedger", "LedgerID", insertedLedgerId, "Insert");
                await ExecuteAsync(c, null, @"
UPDATE StudentFeeLedger
SET TotalPaidAmount = TotalPaidAmount + @p0
WHERE TermID = @p1 AND StudentID = @p2 AND SchoolId = @p3",
                    amountPaid, termId, studentId, TenantContext.RequireSchoolId());
                var ledgerId = await FindLedgerIdAsync(c, termId, studentId);
                if (ledgerId > 0) await TryRecordSyncUpsertAsync("StudentFeeLedger", "LedgerID", ledgerId, "Update");
            }
        }

        public async Task<StudentBillingBreakdown> GetStudentBillingBreakdownAsync(string studentId, int? termId = null)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var targetTermId = termId ?? await GetActiveTermIdAsync(c);
                if (targetTermId == null) return null;

                using (var cmd = new SqlCommand(@"
SELECT TOP 1 l.StudentID, l.TermID, t.TermName,
       ISNULL(l.PreviousBalance, 0) AS PreviousBalance,
       ISNULL(l.CurrentTermCharge, l.TotalExpectedAmount) AS CurrentTermCharge,
       l.TotalExpectedAmount,
       l.TotalPaidAmount
FROM StudentFeeLedger l
LEFT JOIN AcademicTerms t ON t.TermID = l.TermID
WHERE CAST(l.StudentID AS NVARCHAR(50)) IN (@p0, @p1)
  AND l.TermID = @p2
  AND l.SchoolId = @p3", c))
                {
                    var ids = BuildStudentIdCandidates(studentId);
                    cmd.AddPositionalParameter(ids[0]);
                    cmd.AddPositionalParameter(ids[1]);
                    cmd.AddPositionalParameter( targetTermId.Value);
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        if (!await r.ReadAsync()) return null;
                        return new StudentBillingBreakdown
                        {
                            StudentID = r["StudentID"].ToString(),
                            TermID = Convert.ToInt32(r["TermID"]),
                            TermName = r["TermName"] == DBNull.Value ? "" : r["TermName"].ToString(),
                            PreviousBalance = Convert.ToDecimal(r["PreviousBalance"]),
                            CurrentTermFee = Convert.ToDecimal(r["CurrentTermCharge"]),
                            TotalExpected = Convert.ToDecimal(r["TotalExpectedAmount"]),
                            AmountPaid = Convert.ToDecimal(r["TotalPaidAmount"])
                        };
                    }
                }
            }
        }

        public async Task<TermClosureSummary> GetClosureSummaryAsync(int termId)
        {
            var term = await GetTermByIdAsync(termId);
            if (term == null) throw new InvalidOperationException("Term not found.");

            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                bool paymentHasTerm = await HasColumnAsync(c, "payment_record", "TermID");

                var summary = new TermClosureSummary { Term = term };
                summary.TotalStudents = await CountAsync(c, "Students", "1=1");
                summary.NewAdmissions = await CountAsync(c, "Students", "admission_date BETWEEN @p0 AND @p1", term.StartDate.Date, term.EndDate.Date);
                summary.TotalEmployees = await CountAsync(c, "Employee", "1=1");
                summary.NewEmployees = await CountAsync(c, "Employee", "[date] BETWEEN @p0 AND @p1", term.StartDate.Date, term.EndDate.Date);
                summary.TotalExpectedFees = await ScalarDecimalAsync(c, "SELECT ISNULL(SUM(TotalExpectedAmount),0) FROM StudentFeeLedger WHERE TermID = @p0 AND SchoolId = @p1", termId, TenantContext.RequireSchoolId());
                summary.TotalOutstandingFees = await ScalarDecimalAsync(c, "SELECT ISNULL(SUM(TotalExpectedAmount - TotalPaidAmount),0) FROM StudentFeeLedger WHERE TermID = @p0 AND SchoolId = @p1", termId, TenantContext.RequireSchoolId());
                summary.TotalCollectedFees = paymentHasTerm
                    ? await ScalarDecimalAsync(c, "SELECT ISNULL(SUM(Amount_paid),0) FROM payment_record WHERE TermID = @p0 AND SchoolId = @p1", termId, TenantContext.RequireSchoolId())
                    : await ScalarDecimalAsync(c, "SELECT ISNULL(SUM(Amount_paid),0) FROM payment_record WHERE [Date] BETWEEN @p0 AND @p1 AND SchoolId = @p2", term.StartDate.Date, term.EndDate.Date, TenantContext.RequireSchoolId());
                return summary;
            }
        }

        public async Task<AcademicTerm> GetTermByIdAsync(int termId)
        {
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand(@"
SELECT t.TermID, t.AcademicYearID, y.YearName, t.TermName, t.StartDate, t.EndDate, t.ReopeningDate,
       t.IsActive, t.IsClosed, t.ClosedAt, t.ClosureReportPath
FROM AcademicTerms t
LEFT JOIN AcademicYears y ON y.AcademicYearID = t.AcademicYearID
WHERE t.TermID = @p0 AND t.SchoolId = @p1", c))
                {
                    cmd.AddPositionalParameter( termId);
                    TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        return await r.ReadAsync() ? MapTerm(r) : null;
                    }
                }
            }
        }

        private static async Task<int> EnsureStudentLedgerAsync(SqlConnection c, int termId, string studentId, decimal expectedAmount)
        {
            using (var exists = new SqlCommand(@"
SELECT TOP 1 LedgerID FROM StudentFeeLedger
WHERE TermID = @p0 AND StudentID = @p1 AND SchoolId = @p2", c))
            {
                exists.AddPositionalParameter( termId);
                exists.AddPositionalParameter( studentId);
                TenantContext.AddSchoolParameter(exists);
                var found = await exists.ExecuteScalarAsync();
                if (found != null && found != DBNull.Value) return 0;
            }

            using (var insert = new SqlCommand(@"
INSERT INTO StudentFeeLedger (StudentID, TermID, PreviousBalance, CurrentTermCharge, TotalExpectedAmount, TotalPaidAmount, SchoolId)
VALUES (@p0, @p1, 0, @p2, @p3, 0, @SchoolId)", c))
            {
                insert.AddPositionalParameter( studentId);
                insert.AddPositionalParameter( termId);
                insert.AddPositionalParameter( expectedAmount);
                insert.AddPositionalParameter( expectedAmount);
                TenantContext.AddSchoolParameter(insert);
                await insert.ExecuteNonQueryAsync();
            }

            using (var id = new SqlCommand("SELECT CAST(@@IDENTITY AS int)", c))
            {
                var ledgerId = await id.ExecuteScalarAsync();
                return ledgerId == null || ledgerId == DBNull.Value ? 0 : Convert.ToInt32(ledgerId);
            }
        }

        private async Task InsertReminderIfMissingAsync(int termId, string reminderType, DateTime sendOn, string message)
        {
            if (sendOn.Date < DateTime.Today) return;
            using (var c = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var exists = new SqlCommand(@"
SELECT TOP 1 ReminderID
FROM TermReminderSchedule
WHERE TermID = @p0 AND ReminderType = @p1 AND SchoolId = @p2", c))
                {
                    exists.AddPositionalParameter( termId);
                    exists.AddPositionalParameter( reminderType);
                    TenantContext.AddSchoolParameter(exists);
                    var found = await exists.ExecuteScalarAsync();
                    if (found != null && found != DBNull.Value) return;
                }

                await ExecuteAsync(c, null, @"
INSERT INTO TermReminderSchedule (TermID, ReminderType, SendOnDate, Message, [Status], SchoolId)
VALUES (@p0, @p1, @p2, @p3, 'Pending', @SchoolId)",
                    termId, reminderType, sendOn.Date, message, TenantContext.RequireSchoolId());
                using (var id = new SqlCommand("SELECT CAST(@@IDENTITY AS int)", c))
                {
                    var reminderId = Convert.ToInt32(await id.ExecuteScalarAsync());
                    if (reminderId > 0) await TryRecordSyncUpsertAsync("TermReminderSchedule", "ReminderID", reminderId, "Insert");
                }
            }
        }

        private static async Task<int> CountAsync(SqlConnection c, string table, string where, params object[] values)
        {
            if (!await TableExistsAsync(c, table)) return 0;
            var tenant = await TenantContext.HasSchoolIdColumnAsync(c, table);
            var sql = $"SELECT COUNT(*) FROM {table} WHERE {where}";
            if (tenant) sql += TenantContext.FilterClauseSql();

            using (var cmd = new SqlCommand(sql, c))
            {
                foreach (var value in values) cmd.AddPositionalParameter( value ?? DBNull.Value);
                if (tenant) TenantContext.AddSchoolParameter(cmd);
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private static async Task<int> FindLedgerIdAsync(SqlConnection c, int termId, string studentId)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 LedgerID
FROM StudentFeeLedger
WHERE TermID = @p0 AND StudentID = @p1 AND SchoolId = @p2", c))
            {
                cmd.AddPositionalParameter(termId);
                cmd.AddPositionalParameter(studentId);
                TenantContext.AddSchoolParameter(cmd);
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private async Task TryRecordSyncUpsertAsync(string tableName, string primaryKeyName, object primaryKeyValue, string operation)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(tableName, primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Academic session sync capture skipped: " + ex.Message);
            }
        }

        private static async Task<decimal> ScalarDecimalAsync(SqlConnection c, string sql, params object[] values)
        {
            using (var cmd = new SqlCommand(sql, c))
            {
                foreach (var value in values) cmd.AddPositionalParameter( value ?? DBNull.Value);
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        private static async Task<int?> GetActiveTermIdAsync(SqlConnection c)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 TermID
FROM AcademicTerms
WHERE IsActive = 1 AND IsClosed = 0 AND SchoolId = @p0
ORDER BY StartDate DESC, TermID DESC", c))
            {
                TenantContext.AddSchoolParameter(cmd);
                var raw = await cmd.ExecuteScalarAsync();
                return raw == null || raw == DBNull.Value ? (int?)null : Convert.ToInt32(raw);
            }
        }

        private static string[] BuildStudentIdCandidates(string studentId)
        {
            var raw = (studentId ?? "").Trim();
            var numeric = StudentId.Parse(raw);
            var display = StudentId.Display(string.IsNullOrWhiteSpace(numeric) ? raw : numeric);
            if (string.Equals(numeric, display, StringComparison.OrdinalIgnoreCase))
            {
                display = raw;
            }
            if (string.IsNullOrWhiteSpace(display))
            {
                display = numeric;
            }
            return new[] { numeric, display };
        }

        private static async Task<bool> TableExistsAsync(SqlConnection c, string table)
        {
            var safeTable = table.Replace("'", "''");
            using (var cmd = new SqlCommand($"SELECT OBJECT_ID(N'{safeTable}')", c))
            {
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static async Task<bool> HasColumnAsync(SqlConnection c, string table, string column)
        {
            var safeTable = table.Replace("'", "''");
            var safeColumn = column.Replace("'", "''");
            using (var cmd = new SqlCommand($"SELECT COL_LENGTH('{safeTable}', '{safeColumn}')", c))
            {
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static async Task ExecuteAsync(SqlConnection c, SqlTransaction tx, string sql, params object[] values)
        {
            using (var cmd = new SqlCommand(sql, c))
            {
                if (tx != null) cmd.Transaction = tx;
                foreach (var value in values) cmd.AddPositionalParameter( value ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static AcademicYear MapYear(IDataRecord r) => new AcademicYear
        {
            AcademicYearID = Convert.ToInt32(r["AcademicYearID"]),
            YearName = r["YearName"].ToString(),
            StartDate = Convert.ToDateTime(r["StartDate"]),
            EndDate = Convert.ToDateTime(r["EndDate"]),
            IsActive = ToBool(r["IsActive"])
        };

        private static AcademicTerm MapTerm(IDataRecord r) => new AcademicTerm
        {
            TermID = Convert.ToInt32(r["TermID"]),
            AcademicYearID = Convert.ToInt32(r["AcademicYearID"]),
            AcademicYearName = r["YearName"] == DBNull.Value ? "" : r["YearName"].ToString(),
            TermName = r["TermName"].ToString(),
            StartDate = Convert.ToDateTime(r["StartDate"]),
            EndDate = Convert.ToDateTime(r["EndDate"]),
            ReopeningDate = HasColumn(r, "ReopeningDate") && r["ReopeningDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["ReopeningDate"]) : null,
            IsActive = ToBool(r["IsActive"]),
            IsClosed = ToBool(r["IsClosed"]),
            ClosedAt = r["ClosedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ClosedAt"]),
            ClosureReportPath = r["ClosureReportPath"] == DBNull.Value ? "" : r["ClosureReportPath"].ToString()
        };

        private static bool ToBool(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool b) return b;
            if (value is byte by) return by != 0;
            if (value is int i) return i != 0;
            bool parsed;
            return bool.TryParse(value.ToString(), out parsed) && parsed;
        }

        private static bool HasColumn(IDataRecord record, string columnName)
        {
            for (int i = 0; i < record.FieldCount; i++)
            {
                if (string.Equals(record.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}

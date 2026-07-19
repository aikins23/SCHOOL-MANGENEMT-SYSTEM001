using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace Kingdom.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return MainAsync(args ?? new string[0]).GetAwaiter().GetResult();
        }

        private static async Task<int> MainAsync(string[] args)
        {
            if (IsLiveSmokeRun(args))
            {
                return await RunLiveDesktopSmokeAsync();
            }

            var tests = new List<TestCase>
            {
                new TestCase("PhoneNumberGh normalizes local Ghana mobile numbers", PhoneNumberGh_NormalizesLocalNumbers),
                new TestCase("PhoneNumberGh rejects invalid numbers", PhoneNumberGh_RejectsInvalidNumbers),
                new TestCase("SmsSenderIds builds expected sender IDs", SmsSenderIds_BuildsExpectedIds),
                new TestCase("SmsSenderIds detects names over gateway limit", SmsSenderIds_DetectsGatewayLimit),
                new TestCase("StudentId formats numeric IDs as prefixed display", StudentId_FormatsDisplayIds),
                new TestCase("StudentId parses display IDs back to numeric", StudentId_ParsesDisplayIds),
                new TestCase("TransportPeriod maps route terms to current period", TransportPeriod_MapsTermsToPeriod),
                new TestCase("SmsOutboxKey is deterministic and content-sensitive", SmsOutboxKey_IsDeterministic),
                new TestCase("ImportCredentials derives usernames and strong passwords", ImportCredentials_DerivesUsernamesAndPasswords),
                new TestCase("ExpenseAmount parses legacy varchar and round-trips", ExpenseAmount_ParsesAndStores),
                new TestCase("FinancePeriod resolves term/year/all-time/custom windows", FinancePeriod_ResolvesWindows),
                new TestCase("SyncSchema adds sync columns idempotently", SyncSchema_AddsColumnsIdempotentlyAsync, isIntegration: true),
                new TestCase("SyncSchema creates performance indexes idempotently", SyncSchema_CreatesPerformanceIndexesIdempotentlyAsync, isIntegration: true),
                new TestCase("TenantSchema covers core school-owned tables", TenantSchema_CoversCoreTables),
                new TestCase("Production code contains no Access or OleDb data access", ProductionCode_ContainsNoAccessOrOleDbDataAccess),
                new TestCase("SchoolInformation creates a school identity", SchoolInformation_CreatesSchoolIdentity),
                new TestCase("SecretStorage protects and restores local secrets", SecretStorage_ProtectsAndRestoresSecrets),
                new TestCase("SecretStorage preserves legacy plaintext values", SecretStorage_PreservesLegacyPlaintextValues),
                new TestCase("DatabaseConnectionSettings prefers environment configuration", DatabaseConnectionSettings_PrefersEnvironmentConfiguration),
                new TestCase("DatabaseConnectionSettings protects local connection files", DatabaseConnectionSettings_ProtectsLocalConnectionFiles),
                new TestCase("DatabaseConnectionSettings rejects insecure production transport", DatabaseConnectionSettings_RejectsInsecureProductionTransport),
                new TestCase("FeeBalanceCalculator calculates remaining balances", FeeBalanceCalculator_CalculatesRemainingBalances),
                new TestCase("FeeBalanceCalculator rejects invalid values", FeeBalanceCalculator_RejectsInvalidValues),
                new TestCase("PaymentService records calculated payment balances", PaymentService_RecordsCalculatedPaymentBalancesAsync),
                new TestCase("PaymentService records overpayments as zero balance", PaymentService_RecordsOverpaymentsAsZeroBalanceAsync),
                new TestCase("PaymentService validates required payment fields", PaymentService_ValidatesRequiredPaymentFieldsAsync),
                new TestCase("PaymentService reports repository save failures", PaymentService_ReportsRepositorySaveFailuresAsync),
                new TestCase("AdditionalFeeService previews and posts approved flat fees", AdditionalFeeService_PreviewsAndPostsFlatFeesAsync, isIntegration: true),
                new TestCase("AdditionalFeeService queues parent SMS on approved additional fees", AdditionalFeeService_QueuesParentSmsOnApprovalAsync, isIntegration: true),
                new TestCase("AdditionalFeeService targets class and department amounts", AdditionalFeeService_TargetsClassAndDepartmentAmountsAsync, isIntegration: true),
                new TestCase("AdditionalFeeService blocks duplicate open fees", AdditionalFeeService_BlocksDuplicateOpenFeesAsync, isIntegration: true),
                new TestCase("StudentService maps tuition fees by class", StudentService_MapsTuitionFeesByClass),
                new TestCase("StudentService maps every configured class to a non-default fee", StudentService_MapsEveryConfiguredClass),
                new TestCase("StudentService creates opening fee records on add", StudentService_CreatesOpeningFeeRecordsOnAddAsync),
                new TestCase("StudentService rejects invalid students before persistence", StudentService_RejectsInvalidStudentsBeforePersistenceAsync),
                new TestCase("StudentService rejects duplicate student IDs before fee creation", StudentService_RejectsDuplicateStudentIdsBeforeFeeCreationAsync),
                new TestCase("StudentService updates fee records on class change", StudentService_UpdatesFeeRecordsOnClassChangeAsync),
                new TestCase("StudentService promotes selected students and refreshes fee records", StudentService_PromotesSelectedStudentsAndRefreshesFeeRecordsAsync),
                new TestCase("DraftAdmissionService approves draft into student and fee records", DraftAdmissionService_ApprovesDraftIntoStudentAndFeeRecordsAsync, isIntegration: true),
                new TestCase("SmsService queues approved admission messages without live keys", SmsService_QueuesAdmissionSmsWithoutLiveKeysAsync, isIntegration: true),
                new TestCase("LeaveRequest calculates inclusive duration", LeaveRequest_CalculatesInclusiveDuration),
                new TestCase("Leave term calendar maps school terms", LeaveTermCalendar_MapsSchoolTerms),
                new TestCase("LeaveService rejects invalid leave ranges", LeaveService_RejectsInvalidLeaveRangesAsync),
                new TestCase("LeaveService rejects overlapping approved leave", LeaveService_RejectsOverlappingApprovedLeaveAsync),
                new TestCase("LeaveService submits pending leave requests", LeaveService_SubmitsPendingLeaveRequestsAsync),
                new TestCase("LeaveService calculates term leave balance", LeaveService_CalculatesTermLeaveBalanceAsync),
                new TestCase("LeaveService filters request tables by status", LeaveService_FiltersRequestTablesByStatusAsync),
                new TestCase("ExamResult calculates total score grade and remark", ExamResult_CalculatesTotalScoreGradeAndRemark),
                new TestCase("ExamResult maps grade boundaries", ExamResult_MapsGradeBoundaries),
                new TestCase("ExamService adds new calculated results", ExamService_AddsNewCalculatedResultsAsync),
                new TestCase("ExamService updates existing calculated results", ExamService_UpdatesExistingCalculatedResultsAsync),
                new TestCase("ExamService reports repository save errors", ExamService_ReportsRepositorySaveErrorsAsync),
                new TestCase("ReportCardPDFGenerator creates a valid PDF", ReportCardPDFGenerator_CreatesValidPdfAsync),
                new TestCase("AuthService hashes and verifies passwords", AuthService_HashesAndVerifiesPasswords),
                new TestCase("AuthService rejects invalid login credentials", AuthService_RejectsInvalidLoginCredentials),
                new TestCase("AuthService validates registration input", AuthService_ValidatesRegistrationInput),
                new TestCase("AuthService explains missing login accounts", AuthService_ExplainsMissingLoginAccountsAsync, isIntegration: true),
                new TestCase("AuthService blocks protected screens while logged out", AuthService_BlocksProtectedScreensWhileLoggedOut),
                new TestCase("AuthService denies blank and unknown screen keys", AuthService_DeniesBlankAndUnknownScreenKeys),
                new TestCase("AuthService enforces role-specific screen access", AuthService_EnforcesRoleSpecificScreenAccess),
                new TestCase("ValidationHelper validates common school form inputs", ValidationHelper_ValidatesCommonInputs),
                new TestCase("EmployeeRepository integration filters by department", EmployeeRepository_FiltersByDepartmentAsync, isIntegration: true),
                new TestCase("EmployeeRepository integration treats malicious filters as literals", EmployeeRepository_TreatsMaliciousFiltersAsLiteralsAsync, isIntegration: true),
                new TestCase("EmployeeRepository integration retrieves employee by ID", EmployeeRepository_RetrievesEmployeeByIdAsync, isIntegration: true),
                new TestCase("StudentRepository integration round-trips student records", StudentRepository_RoundTripsStudentRecordsAsync, isIntegration: true),
                new TestCase("StudentRepository integration returns student identity when triggers insert rows", StudentRepository_ReturnsStudentIdentityWithInsertTriggersAsync, isIntegration: true),
                new TestCase("FeeRepository integration records fees and payments", FeeRepository_RecordsFeesAndPaymentsAsync, isIntegration: true),
                new TestCase("ExamRepository integration round-trips exam results", ExamRepository_RoundTripsExamResultsAsync, isIntegration: true),
                new TestCase("AttendanceRepository integration saves and analyzes attendance", AttendanceRepository_SavesAndAnalyzesAttendanceAsync, isIntegration: true),
                new TestCase("SubjectRepository integration manages class subjects", SubjectRepository_ManagesClassSubjectsAsync, isIntegration: true),
                new TestCase("ClassRepository integration manages classes and assignments", ClassRepository_ManagesClassesAndAssignmentsAsync, isIntegration: true),
                new TestCase("DashboardRepository integration returns core metrics", DashboardRepository_ReturnsCoreMetricsAsync, isIntegration: true),
                new TestCase("Dashboard summaries refresh monthly metrics", DashboardSummaryRepository_RefreshesMonthlyMetricsAsync, isIntegration: true),
                new TestCase("NoticeRepository saves notices and loads upgraded history", NoticeRepository_SavesAndLoadsHistoryAsync, isIntegration: true),
                new TestCase("Timetable default periods match common school day", Timetable_DefaultPeriodsMatchCommonSchoolDay),
                new TestCase("Timetable departments map JHS classes together", TimetableDepartments_MapJuniorHighClasses),
                new TestCase("Timetable generation explains insufficient teaching slots", TimetableGeneration_ExplainsInsufficientSlotsAsync, isIntegration: true),
                new TestCase("Timetable department generation saves all classes without teacher conflicts", TimetableGeneration_SavesDepartmentBatchesAsync, isIntegration: true)
            };

            bool unitOnly = IsUnitOnlyRun(args);
            if (unitOnly) tests.RemoveAll(test => test.IsIntegration);

            int passed = 0;
            int skipped = 0;
            Console.WriteLine("Kingdom Preparatory test suite");
            Console.WriteLine(unitOnly ? "Mode: unit-only" : "Mode: full");
            Console.WriteLine(new string('-', 38));

            foreach (var test in tests)
            {
                try
                {
                    Console.WriteLine("[RUN ] " + test.Name);
                    await test.Run();
                    passed++;
                    Console.WriteLine("[PASS] " + test.Name);
                }
                catch (SkipTestException ex)
                {
                    skipped++;
                    Console.WriteLine("[SKIP] " + test.Name);
                    Console.WriteLine("       " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[FAIL] " + test.Name);
                    Console.WriteLine("       " + ex.Message);
                }
            }

            Console.WriteLine(new string('-', 38));
            Console.WriteLine($"{passed}/{tests.Count} tests passed. {skipped} skipped.");
            return passed + skipped == tests.Count ? 0 : 1;
        }

        private static bool IsUnitOnlyRun(string[] args)
        {
            foreach (var arg in args ?? new string[0])
            {
                if (string.Equals(arg, "--unit-only", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--no-integration", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            string env = Environment.GetEnvironmentVariable("KINGDOM_TEST_UNIT_ONLY");
            return string.Equals(env, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(env, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLiveSmokeRun(string[] args)
        {
            foreach (var arg in args ?? new string[0])
            {
                if (string.Equals(arg, "--live-smoke", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--desktop-live-smoke", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<int> RunLiveDesktopSmokeAsync()
        {
            string connectionString = kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(
                kingdom_Preparatory_School_Management_System.Common.AppConfig.ConnectionString);
            string marker = "SMOKE" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string smokeClassId = "SMOKE-" + marker.Substring(marker.Length - 8);
            string outputDirectory = Path.Combine(FindRepositoryRoot(), "tmp", "desktop-live-smoke", marker);
            Directory.CreateDirectory(outputDirectory);

            var createdStudentIds = new List<string>();
            int passed = 0;
            int total = 0;

            Console.WriteLine("Nyansapo desktop live smoke pass");
            Console.WriteLine("Database: " + GetDatabaseName(connectionString));
            Console.WriteLine("Marker: " + marker);
            Console.WriteLine(new string('-', 44));

            try
            {
                await RunLiveSmokeStepAsync("Login", async () =>
                {
                    var login = await AuthService.LoginAsync("admin", "admin");
                    AssertEx.True(login.Success, login.Message);
                    AssertEx.Equal(AuthService.UserRole.Administrator, AuthService.CurrentUser.Role);
                }, () => { total++; passed++; });

                Student registeredStudent = null;
                await RunLiveSmokeStepAsync("Add student", async () =>
                {
                    var studentService = CreateLiveStudentService(connectionString);
                    registeredStudent = CreateSmokeStudent(marker, "BASIC 1");
                    var add = await studentService.AddStudentAsync(registeredStudent, skipAgeCheck: true);
                    AssertEx.True(add.Success, add.Message);
                    AssertEx.False(string.IsNullOrWhiteSpace(registeredStudent.StudentID), "Student registration did not return a StudentID.");
                    createdStudentIds.Add(registeredStudent.StudentID);

                    var saved = await new StudentRepository(connectionString).GetByIdAsync(registeredStudent.StudentID);
                    AssertEx.NotNull(saved);
                    AssertEx.Equal(registeredStudent.LastName, saved.LastName);
                }, () => { total++; passed++; });

                Student admittedStudent = null;
                await RunLiveSmokeStepAsync("Admission approval", async () =>
                {
                    var feeRepository = new FeeRepository(connectionString);
                    var draftService = new DraftAdmissionService(
                        new DraftAdmissionRepository(connectionString),
                        CreateLiveStudentService(connectionString),
                        feeRepository);

                    var draft = CreateValidDraftAdmission();
                    draft.FirstName = "SMOKE";
                    draft.LastName = "Admission";
                    draft.Email = "smoke." + marker.ToLowerInvariant() + "@example.com";
                    draft.GuardianEmail = "guardian." + marker.ToLowerInvariant() + "@example.com";
                    draft.GuardianName = "Smoke Guardian";
                    draft.EmergencyContact = "0241234567";
                    draft.SubmittedBy = "Live Smoke";
                    draft.SubmittedDate = DateTime.Now;

                    var create = await draftService.CreateDraftAsync(draft);
                    AssertEx.True(create.Ok, create.Message);

                    int? smokeDraftId = await GetDraftAdmissionIdByEmailAsync(connectionString, draft.Email);
                    AssertEx.True(smokeDraftId.HasValue, "Draft admission was saved, but could not be found by its smoke email.");

                    var approval = await draftService.ApproveAsync(smokeDraftId.Value, "Live Smoke Bursar");
                    AssertEx.True(approval.Ok, approval.Message);
                    AssertEx.NotNull(approval.Student);
                    AssertEx.False(string.IsNullOrWhiteSpace(approval.Student.StudentID), "Approved admission did not return a StudentID.");
                    admittedStudent = approval.Student;
                    createdStudentIds.Add(admittedStudent.StudentID);

                    var latestBalance = await feeRepository.GetLatestBalanceAsync(admittedStudent.StudentID);
                    AssertEx.True(latestBalance.HasValue, "Admission approval did not create a payment balance.");
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Admission SMS queue", async () =>
                {
                    AssertEx.NotNull(admittedStudent);
                    string body = SmsService.BuildStudentAdmissionMessage(admittedStudent, AdmissionFees.Amount, 500m, 1000m);
                    var outbox = new SmsOutboxRepository(connectionString);
                    int outboxId = await outbox.EnqueueAsync("233241234567", SmsSenderIds.StudentAdmission, body + Environment.NewLine + marker);
                    AssertEx.True(outboxId > 0, "Admission SMS was not queued.");
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Fee payment", async () =>
                {
                    AssertEx.NotNull(registeredStudent);
                    var feeRepository = new FeeRepository(connectionString);
                    decimal currentBalance = await feeRepository.GetLatestBalanceAsync(registeredStudent.StudentID) ?? 0m;
                    var payment = await new PaymentService(feeRepository).RecordPaymentAsync(new PaymentRecordRequest
                    {
                        StudentId = registeredStudent.StudentID,
                        ClassId = registeredStudent.ClassID,
                        StudentName = registeredStudent.FullName,
                        CurrentBalance = currentBalance,
                        AmountPaid = 1m,
                        PaymentMode = "Cash",
                        BursarName = "Live Smoke Bursar",
                        PaymentDate = DateTime.Today
                    });

                    AssertEx.True(payment.Success, payment.Message);
                    var latestBalance = await feeRepository.GetLatestBalanceAsync(registeredStudent.StudentID);
                    AssertEx.Equal(payment.NewBalance, latestBalance.Value);
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Send notice", async () =>
                {
                    var notice = new Notice
                    {
                        Title = "Live Smoke Notice " + marker,
                        Message = "This is an automated smoke test notice.",
                        Target = "Specific Class",
                        TargetClass = smokeClassId,
                        Channel = "Email",
                        SentBy = "Live Smoke",
                        SentDate = DateTime.Now
                    };

                    var sent = await new NoticeService(new NoticeRepository(connectionString)).SendNoticeAsync(notice);
                    AssertEx.True(sent.Success, sent.Message);
                    AssertEx.True(notice.NoticeID > 0, "Notice did not save to database.");
                    AssertEx.Contains("no matching recipients", sent.Message);
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Grading", async () =>
                {
                    AssertEx.NotNull(registeredStudent);
                    var result = new ExamResult
                    {
                        StudentId = registeredStudent.StudentID,
                        StudentName = registeredStudent.FullName,
                        ClassId = registeredStudent.ClassID,
                        Subject = "MATHEMATICS",
                        Term = "TERM 1",
                        Year = "2026",
                        Category1 = 30m,
                        Category2 = 10m,
                        Category3 = 10m,
                        ExamScore = 90m
                    };

                    var saved = await new ExamService(new ExamRepository(connectionString)).SaveResultsAsync(new[] { result });
                    AssertEx.True(saved.Success, saved.Message);
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Report card generation", async () =>
                {
                    AssertEx.NotNull(registeredStudent);
                    var manager = new ReportCardManager(
                        new ReportCardDataService(connectionString, new StudentTermRemarksRepository(connectionString)),
                        new ReportCardPDFGenerator(),
                        new ReportCardPrinter());

                    string savedPath = await manager.GenerateAndOutputAsync(
                        registeredStudent.StudentID,
                        "TERM 1",
                        "2026",
                        new ReportCardOutputAction { Type = OutputType.Save, SavePath = outputDirectory });

                    AssertEx.True(File.Exists(savedPath), "Report card PDF was not saved: " + savedPath);
                    AssertEx.True(new FileInfo(savedPath).Length > 0, "Report card PDF is empty.");
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Promotion", async () =>
                {
                    AssertEx.NotNull(registeredStudent);
                    var promotion = await CreateLiveStudentService(connectionString).PromoteStudentsAsync(
                        new[] { registeredStudent.StudentID },
                        "BASIC 2");
                    AssertEx.True(promotion.Success, promotion.Message);

                    var promoted = await new StudentRepository(connectionString).GetByIdAsync(registeredStudent.StudentID);
                    AssertEx.Equal("BASIC 2", promoted.ClassID);
                }, () => { total++; passed++; });

                await RunLiveSmokeStepAsync("Timetable save/load", async () =>
                {
                    var repository = new TimetableRepository(connectionString);
                    var periods = (await repository.GetPeriodsAsync()).Where(p => !p.IsBreak).OrderBy(p => p.SortOrder).ToList();
                    AssertEx.True(periods.Count > 0, "No non-break teaching periods are saved in Period Setup.");

                    var entries = new List<TimetableEntry>
                    {
                        new TimetableEntry
                        {
                            ClassID = smokeClassId,
                            DayOfWeek = 1,
                            PeriodID = periods[0].PeriodID,
                            SubjectName = "SMOKE SUBJECT",
                            TeacherID = null
                        }
                    };

                    await repository.SaveTimetableBatchAsync(smokeClassId, entries);
                    var loaded = await repository.GetTimetableAsync(smokeClassId);
                    AssertEx.Equal(1, loaded.Count);
                    AssertEx.Equal("SMOKE SUBJECT", loaded[0].SubjectName);
                }, () => { total++; passed++; });

                Console.WriteLine(new string('-', 44));
                Console.WriteLine($"{passed}/{total} live smoke steps passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FAIL] " + ex.Message);
                Console.WriteLine(new string('-', 44));
                Console.WriteLine($"{passed}/{Math.Max(total, passed + 1)} live smoke steps passed.");
                return 1;
            }
            finally
            {
                try
                {
                    await CleanupLiveSmokeDataAsync(connectionString, marker, createdStudentIds, smokeClassId);
                    Console.WriteLine("Cleanup: smoke database rows removed.");
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine("Cleanup warning: " + cleanupEx.Message);
                }

                AuthService.Logout();
            }
        }

        private static async Task RunLiveSmokeStepAsync(string name, Func<Task> run, Action passed)
        {
            Console.WriteLine("[RUN ] " + name);
            await run();
            passed();
            Console.WriteLine("[PASS] " + name);
        }

        private static StudentService CreateLiveStudentService(string connectionString)
        {
            var feeRepository = new FeeRepository(connectionString);
            return new StudentService(new StudentRepository(connectionString), feeRepository);
        }

        private static Student CreateSmokeStudent(string marker, string classId)
        {
            return new Student
            {
                FirstName = "SMOKE",
                LastName = "Student",
                DateOfBirth = new DateTime(2015, 1, 1),
                Gender = "MALE",
                ClassID = classId,
                Email = "student." + marker.ToLowerInvariant() + "@example.com",
                HomeTown = "Akim Oda",
                Residence = "Abenase",
                Allergies = "",
                GuardianName = "Smoke Guardian",
                GuardianEmail = "guardian." + marker.ToLowerInvariant() + "@example.com",
                GuardianLocation = "Abenase",
                EmergencyContact = "0241234567",
                ProfilePhoto = new byte[0],
                AdmissionDate = DateTime.Today
            };
        }

        private static string GetDatabaseName(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "(not specified)" : builder.InitialCatalog;
            }
            catch
            {
                return "(could not parse connection string)";
            }
        }

        private static async Task<int?> GetDraftAdmissionIdByEmailAsync(string connectionString, string email)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"
SELECT TOP 1 DraftID
FROM DraftAdmissions
WHERE Email = @Email
ORDER BY DraftID DESC;", connection))
            {
                command.Parameters.AddWithValue("@Email", email ?? "");
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }

        private static async Task CleanupLiveSmokeDataAsync(
            string connectionString,
            string marker,
            IEnumerable<string> studentIds,
            string smokeClassId)
        {
            string idList = string.Join(",", (studentIds ?? new string[0])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select((id, index) => "@StudentId" + index));

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(BuildLiveSmokeCleanupSql(idList), connection))
                {
                    int i = 0;
                    foreach (var id in (studentIds ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        command.Parameters.AddWithValue("@StudentId" + i, id);
                        i++;
                    }

                    command.Parameters.AddWithValue("@Marker", "%" + marker + "%");
                    command.Parameters.AddWithValue("@SmokeClassId", smokeClassId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static string BuildLiveSmokeCleanupSql(string studentIdList)
        {
            string studentFilter = string.IsNullOrWhiteSpace(studentIdList)
                ? "1 = 0"
                : "CAST(StudentID AS NVARCHAR(50)) IN (" + studentIdList + ")";

            return @"
DECLARE @SmokeStudents TABLE (StudentID NVARCHAR(50) NOT NULL PRIMARY KEY);

IF OBJECT_ID(N'Students', N'U') IS NOT NULL
BEGIN
    INSERT INTO @SmokeStudents(StudentID)
    SELECT DISTINCT CAST(StudentID AS NVARCHAR(50))
    FROM Students
    WHERE " + studentFilter + @"
       OR Email LIKE @Marker
       OR GuidianceEmail LIKE @Marker
       OR Email LIKE 'student.smoke%@example.com'
       OR GuidianceEmail LIKE 'guardian.smoke%@example.com'
       OR (FirstName = 'SMOKE' AND LastName = 'Student');
END

IF OBJECT_ID(N'TimetableEntries', N'U') IS NOT NULL
    DELETE FROM TimetableEntries WHERE ClassID = @SmokeClassId;

IF OBJECT_ID(N'Notices', N'U') IS NOT NULL
    DELETE FROM Notices WHERE Title LIKE @Marker OR [Message] LIKE @Marker;

IF OBJECT_ID(N'SmsOutbox', N'U') IS NOT NULL
    DELETE FROM SmsOutbox WHERE [Message] LIKE @Marker;

IF OBJECT_ID(N'StudentTermRemarks', N'U') IS NOT NULL
    DELETE FROM StudentTermRemarks WHERE CAST(StudentID AS NVARCHAR(50)) IN (SELECT StudentID FROM @SmokeStudents);

IF OBJECT_ID(N'examss', N'U') IS NOT NULL
    DELETE FROM examss WHERE CAST(std_id AS NVARCHAR(50)) IN (SELECT StudentID FROM @SmokeStudents);

IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL
    DELETE FROM payment_record WHERE CAST(StudentID AS NVARCHAR(50)) IN (SELECT StudentID FROM @SmokeStudents);

IF OBJECT_ID(N'fees', N'U') IS NOT NULL
    DELETE FROM fees WHERE CAST(StudentID AS NVARCHAR(50)) IN (SELECT StudentID FROM @SmokeStudents);

IF OBJECT_ID(N'DraftAdmissions', N'U') IS NOT NULL
    DELETE FROM DraftAdmissions WHERE LastName LIKE @Marker OR FirstName LIKE @Marker OR Email LIKE @Marker OR GuidianceEmail LIKE @Marker;

IF OBJECT_ID(N'Attendance', N'U') IS NOT NULL
    DELETE FROM Attendance WHERE ReferenceID IN (SELECT StudentID FROM @SmokeStudents);

IF OBJECT_ID(N'Students', N'U') IS NOT NULL
    DELETE FROM Students WHERE CAST(StudentID AS NVARCHAR(50)) IN (SELECT StudentID FROM @SmokeStudents);";
        }

        private static void PhoneNumberGh_NormalizesLocalNumbers()
        {
            AssertEx.Equal("233241234567", PhoneNumberGh.NormalizeGh("0241234567"));
            AssertEx.Equal("233241234567", PhoneNumberGh.NormalizeGh("+233241234567"));
            AssertEx.Equal("233241234567", PhoneNumberGh.NormalizeGh("233241234567"));
            AssertEx.Equal("233241234567", PhoneNumberGh.NormalizeGh("024 123 4567"));
            AssertEx.Equal("233241234567", PhoneNumberGh.NormalizeGh("024-123-4567"));
            AssertEx.Equal("233541234567", PhoneNumberGh.NormalizeGh("541234567"));
        }

        private static void PhoneNumberGh_RejectsInvalidNumbers()
        {
            AssertEx.Null(PhoneNumberGh.NormalizeGh(null));
            AssertEx.Null(PhoneNumberGh.NormalizeGh(""));
            AssertEx.Null(PhoneNumberGh.NormalizeGh("   "));
            AssertEx.Null(PhoneNumberGh.NormalizeGh("hello"));
            AssertEx.Null(PhoneNumberGh.NormalizeGh("12345"));
            AssertEx.Null(PhoneNumberGh.NormalizeGh("0301234567"), "Landline-style numbers should not pass mobile SMS validation.");
        }

        private static void SmsSenderIds_BuildsExpectedIds()
        {
            AssertEx.Equal("KPSSTDADM", SmsSenderIds.Build("kps", SmsSenderIds.StudentSuffix));
            AssertEx.Equal("KPSEMPADM", SmsSenderIds.Build("KPS", SmsSenderIds.EmployeeSuffix));
            AssertEx.Equal("KPSNOTICE", SmsSenderIds.Build("KPS", SmsSenderIds.NoticeSuffix));
            // The Fee sender ID intentionally carries a trailing dot ("KPSFEES.") — that is the
            // value approved on BulkSMSGh (see SmsSenderIds.FeeSuffix). Pin it so it isn't lost.
            AssertEx.Equal("KPSFEES.", SmsSenderIds.Build("  kps ", SmsSenderIds.FeeSuffix));
        }

        private static void SmsSenderIds_DetectsGatewayLimit()
        {
            AssertEx.False(SmsSenderIds.ExceedsMaxLength("KPS"));
            AssertEx.True(SmsSenderIds.ExceedsMaxLength("KINGDOM"));
        }

        private static void StudentId_FormatsDisplayIds()
        {
            string ab = StudentId.Abbrev;
            AssertEx.Equal(ab + "9016", StudentId.Display("9016"));
            AssertEx.Equal(ab + "9016", StudentId.Display(9016));            // non-string ids accepted
            AssertEx.Equal(ab + "9016", StudentId.Display("  9016  "));      // trims whitespace
            AssertEx.Equal("", StudentId.Display(null));
            AssertEx.Equal("", StudentId.Display(""));
            AssertEx.Equal("", StudentId.Display("   "));
            // Idempotent: an already-prefixed value is returned unchanged (case-insensitive match).
            AssertEx.Equal(ab + "9016", StudentId.Display(ab + "9016"));
            AssertEx.Equal(ab.ToLowerInvariant() + "9016", StudentId.Display(ab.ToLowerInvariant() + "9016"));
        }

        private static void StudentId_ParsesDisplayIds()
        {
            string ab = StudentId.Abbrev;
            AssertEx.Equal("9016", StudentId.Parse(ab + "9016"));
            AssertEx.Equal("9016", StudentId.Parse("  " + ab.ToLowerInvariant() + "9016  ")); // trims + case-insensitive
            AssertEx.Equal("9016", StudentId.Parse("9016"));                                   // already numeric
            AssertEx.Equal("", StudentId.Parse(null));
            AssertEx.Equal("", StudentId.Parse(""));
            AssertEx.Equal("9016", StudentId.Parse(StudentId.Display("9016")));                // round-trips with Display
        }

        private static void TransportPeriod_MapsTermsToPeriod()
        {
            var m = TransportPeriod.Current("Monthly", new DateTime(2026, 6, 7));
            AssertEx.Equal("2026-06", m.Key);
            AssertEx.Equal(new DateTime(2026, 6, 1), m.Start);
            AssertEx.Equal(new DateTime(2026, 6, 30), m.End);

            var d = TransportPeriod.Current("Daily", new DateTime(2026, 6, 7));
            AssertEx.Equal("2026-06-07", d.Key);
            AssertEx.Equal(new DateTime(2026, 6, 7), d.Start);
            AssertEx.Equal(new DateTime(2026, 6, 7), d.End);

            // Weekly = fixed 2-week block (length 14, deterministic, end = start+13).
            var w = TransportPeriod.Current("Weekly", new DateTime(2026, 6, 7));
            AssertEx.Equal(14, (int)(w.End - w.Start).TotalDays + 1);
            AssertEx.True(w.Start <= new DateTime(2026, 6, 7) && new DateTime(2026, 6, 7) <= w.End,
                "today must fall within its fortnight block");

            AssertEx.True(TransportPeriod.SupportsReminders("Monthly"));
            AssertEx.True(TransportPeriod.SupportsReminders("Weekly"));
            AssertEx.False(TransportPeriod.SupportsReminders("Daily"));
        }

        private static async Task SyncSchema_AddsColumnsIdempotentlyAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await SyncSchema.EnsureSyncColumnsAsync(database.ConnectionString);
                await SyncSchema.EnsureSyncColumnsAsync(database.ConnectionString); // second run must be a no-op

                AssertEx.True(await SyncSchema.TableHasColumnAsync(database.ConnectionString, "Employee", "SyncId"));
                AssertEx.True(await SyncSchema.TableHasColumnAsync(database.ConnectionString, "Employee", "UpdatedAt"));
                AssertEx.True(await SyncSchema.TableHasColumnAsync(database.ConnectionString, "Employee", "RowVersion"));
            }
        }

        private static async Task SyncSchema_CreatesPerformanceIndexesIdempotentlyAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await SyncSchema.EnsurePerformanceIndexesAsync(database.ConnectionString);
                await SyncSchema.EnsurePerformanceIndexesAsync(database.ConnectionString);

                AssertEx.True(await IndexExistsAsync(database.ConnectionString, "Students", "IX_Students_StudentID_Lookup"));
                AssertEx.True(await IndexExistsAsync(database.ConnectionString, "payment_record", "IX_payment_record_Student_Latest"));
                AssertEx.True(await IndexExistsAsync(database.ConnectionString, "examss", "IX_examss_Result_Lookup"));
                AssertEx.True(await IndexExistsAsync(database.ConnectionString, "Attendance", "IX_Attendance_TargetDate_Lookup"));
                AssertEx.True(await IndexExistsAsync(database.ConnectionString, "ClassAssignments", "IX_ClassAssignments_Teacher_Lookup"));
            }
        }

        private static void TenantSchema_CoversCoreTables()
        {
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "Students") >= 0);
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "Employee") >= 0);
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "Users") >= 0);
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "payment_record") >= 0);
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "ClassSubjects") >= 0);
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "TimetableEntries") >= 0);
            AssertEx.True(Array.IndexOf(TenantSchema.TenantTables, "SmsOutbox") >= 0);
        }

        private static void ProductionCode_ContainsNoAccessOrOleDbDataAccess()
        {
            var root = FindRepositoryRoot();
            var forbidden = new[] { "OleDb", "System.Data.OleDb", "Provider=Microsoft", ".accdb", ".mdb", "Microsoft.ACE", "Jet.OLEDB" };
            var sourceFiles = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsUnder(path, root, "bin"))
                .Where(path => !IsUnder(path, root, "obj"))
                .Where(path => !IsUnder(path, root, ".git"))
                .Where(path => !IsUnder(path, root, ".worktrees"))
                .Where(path => !IsUnder(path, root, ".agents"))
                .Where(path => !IsUnder(path, root, "Tests"))
                .Where(path => !IsUnder(path, root, "scratch"))
                .Where(path => !IsUnder(path, root, "tmp"))
                .Where(path => !IsUnder(path, root, "web"))
                .ToList();

            var offenders = new List<string>();
            foreach (var file in sourceFiles)
            {
                var text = File.ReadAllText(file);
                foreach (var token in forbidden)
                {
                    if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(MakeRelativePath(root, file) + " contains " + token);
                    }
                }
            }

            AssertEx.Equal(0, offenders.Count, string.Join(Environment.NewLine, offenders.Take(20)));
        }

        private static void SchoolInformation_CreatesSchoolIdentity()
        {
            var info = new SchoolInformation();
            AssertEx.NotEqual(Guid.Empty, info.SchoolId);
        }

        private static void SmsOutboxKey_IsDeterministic()
        {
            string a = SmsOutboxKey.Compute("233241234567", "KPSFEES.", "Hello");
            string b = SmsOutboxKey.Compute("233241234567", "KPSFEES.", "Hello");
            AssertEx.Equal(a, b);
            AssertEx.Equal(64, a.Length);
            AssertEx.NotEqual(a, SmsOutboxKey.Compute("233241234567", "KPSFEES.", "Hello world"));
            AssertEx.NotEqual(a, SmsOutboxKey.Compute("233240000000", "KPSFEES.", "Hello"));
        }

        private static void ExpenseAmount_ParsesAndStores()
        {
            AssertEx.Equal(1500.00m, ExpenseAmount.Parse("1,500.00"));
            AssertEx.Equal(1500.00m, ExpenseAmount.Parse("GHS 1500"));
            AssertEx.Equal(0m, ExpenseAmount.Parse(""));
            AssertEx.Equal(0m, ExpenseAmount.Parse("abc"));
            AssertEx.Equal("1500.00", ExpenseAmount.Store(1500m));
            AssertEx.Equal(1234.50m, ExpenseAmount.Parse(ExpenseAmount.Store(1234.5m))); // round-trip
        }

        private static void FinancePeriod_ResolvesWindows()
        {
            var d = new DateTime(2026, 6, 15);
            var year = FinancePeriod.Resolve(FinanceWindow.Year, d);
            AssertEx.Equal(new DateTime(2026, 1, 1), year.From);
            AssertEx.Equal(new DateTime(2026, 12, 31), year.To);
            AssertEx.Equal("2026", year.Label);

            var all = FinancePeriod.Resolve(FinanceWindow.AllTime, d);
            AssertEx.True(all.From <= new DateTime(2000, 1, 1) && all.To >= new DateTime(2099, 12, 31), "all-time spans wide");

            var cust = FinancePeriod.Resolve(FinanceWindow.Custom, d, new DateTime(2026, 5, 10), new DateTime(2026, 5, 1));
            AssertEx.Equal(new DateTime(2026, 5, 1), cust.From);   // swapped because from > to
            AssertEx.Equal(new DateTime(2026, 5, 10), cust.To);

            var term = FinancePeriod.Resolve(FinanceWindow.Term, d); // May-Aug term for June
            AssertEx.True(term.From <= d && d <= term.To, "today falls within the resolved term");
        }

        private static void ImportCredentials_DerivesUsernamesAndPasswords()
        {
            string ab = StudentId.Abbrev.ToLowerInvariant();
            AssertEx.Equal(ab + "9016", ImportCredentials.UsernameFor("9016"));
            AssertEx.Equal(ab + "9016", ImportCredentials.UsernameFor(ab.ToUpperInvariant() + "9016")); // display form in
            AssertEx.Equal(ab + "1", ImportCredentials.UsernameFor("1"));                                // short IDs OK
            AssertEx.True(ImportCredentials.UsernameFor("1").Length >= 3, "prefix must satisfy the 3-char username minimum");

            string p1 = ImportCredentials.NewPassword();
            string p2 = ImportCredentials.NewPassword();
            AssertEx.True(ValidationHelper.IsStrongPassword(p1), "password must satisfy the strong-password rule: " + p1);
            AssertEx.Equal(8, p1.Length);
            AssertEx.NotEqual(p1, p2, "consecutive passwords must differ");
        }

        private static void SecretStorage_ProtectsAndRestoresSecrets()
        {
            const string secret = "Arkesel-or-smtp-secret-123!";
            string protectedValue = SecretStorage.Protect(secret);

            AssertEx.NotNull(protectedValue);
            AssertEx.NotEqual(secret, protectedValue);
            AssertEx.True(SecretStorage.IsProtected(protectedValue));
            AssertEx.Equal(secret, SecretStorage.Unprotect(protectedValue));
            AssertEx.Equal(protectedValue, SecretStorage.Protect(protectedValue), "Protecting an already protected value should be idempotent.");
        }

        private static void SecretStorage_PreservesLegacyPlaintextValues()
        {
            AssertEx.Equal("plain-old-secret", SecretStorage.Unprotect("plain-old-secret"));
            AssertEx.Equal("", SecretStorage.Protect(""));
            AssertEx.Equal("", SecretStorage.Unprotect(""));
            AssertEx.False(SecretStorage.IsProtected("plain-old-secret"));
        }

        private static void DatabaseConnectionSettings_PrefersEnvironmentConfiguration()
        {
            const string environmentConnection = "Data Source=env-server;Initial Catalog=EnvDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
            string previousConnection = Environment.GetEnvironmentVariable(DatabaseConnectionSettings.ConnectionEnvironmentVariable);
            string previousEnvironment = Environment.GetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable);
            try
            {
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable, "Development");
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.ConnectionEnvironmentVariable, environmentConnection);

                string resolved = DatabaseConnectionSettings.Resolve(
                    "Data Source=fallback;Initial Catalog=FallbackDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
                var builder = new SqlConnectionStringBuilder(resolved);

                AssertEx.Equal("env-server", builder.DataSource);
                AssertEx.Equal("EnvDb", builder.InitialCatalog);
                AssertEx.False(builder.PersistSecurityInfo);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.ConnectionEnvironmentVariable, previousConnection);
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable, previousEnvironment);
            }
        }

        private static void DatabaseConnectionSettings_ProtectsLocalConnectionFiles()
        {
            string temporaryPath = Path.Combine(Path.GetTempPath(), "nyansapo-connection-" + Guid.NewGuid().ToString("N"));
            string previousConnection = Environment.GetEnvironmentVariable(DatabaseConnectionSettings.ConnectionEnvironmentVariable);
            string previousFile = Environment.GetEnvironmentVariable(DatabaseConnectionSettings.ConnectionFileEnvironmentVariable);
            string previousEnvironment = Environment.GetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable);
            try
            {
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.ConnectionEnvironmentVariable, null);
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.ConnectionFileEnvironmentVariable, temporaryPath);
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable, "Development");

                const string connection = "Data Source=secure-server;Initial Catalog=SecureDb;User ID=app;Password=NeverStoreThisInSource;Encrypt=True;TrustServerCertificate=True";
                DatabaseConnectionSettings.SaveProtected(connection);

                string stored = File.ReadAllText(temporaryPath);
                AssertEx.True(SecretStorage.IsProtected(stored));
                AssertEx.False(stored.Contains("NeverStoreThisInSource"));

                var builder = new SqlConnectionStringBuilder(DatabaseConnectionSettings.Resolve(""));
                AssertEx.Equal("secure-server", builder.DataSource);
                AssertEx.Equal("SecureDb", builder.InitialCatalog);
                AssertEx.Equal("app", builder.UserID);
                AssertEx.Equal("NeverStoreThisInSource", builder.Password);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.ConnectionEnvironmentVariable, previousConnection);
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.ConnectionFileEnvironmentVariable, previousFile);
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable, previousEnvironment);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                if (File.Exists(temporaryPath + ".tmp")) File.Delete(temporaryPath + ".tmp");
            }
        }

        private static void DatabaseConnectionSettings_RejectsInsecureProductionTransport()
        {
            string previousEnvironment = Environment.GetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable);
            try
            {
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable, "Production");

                AssertEx.Throws<InvalidOperationException>(() => DatabaseConnectionSettings.EnsureProductionSecurity(
                    "Data Source=sql.example.com;Initial Catalog=Neat_Academy;Integrated Security=True;Encrypt=False;TrustServerCertificate=False"));
                AssertEx.Throws<InvalidOperationException>(() => DatabaseConnectionSettings.EnsureProductionSecurity(
                    "Data Source=sql.example.com;Initial Catalog=Neat_Academy;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"));

                DatabaseConnectionSettings.EnsureProductionSecurity(
                    "Data Source=sql.example.com;Initial Catalog=Neat_Academy;Integrated Security=True;Encrypt=True;TrustServerCertificate=False");
            }
            finally
            {
                Environment.SetEnvironmentVariable(DatabaseConnectionSettings.EnvironmentNameVariable, previousEnvironment);
            }
        }

        private static void FeeBalanceCalculator_CalculatesRemainingBalances()
        {
            AssertEx.Equal(700m, FeeBalanceCalculator.CalculateNewBalance(1000m, 300m));
            AssertEx.Equal(0m, FeeBalanceCalculator.CalculateNewBalance(1000m, 1000m));
            AssertEx.Equal(0m, FeeBalanceCalculator.CalculateNewBalance(1000m, 1200m));
            AssertEx.Equal(0.25m, FeeBalanceCalculator.CalculateNewBalance(1.00m, 0.75m));
        }

        private static void FeeBalanceCalculator_RejectsInvalidValues()
        {
            AssertEx.Throws<ArgumentOutOfRangeException>(() => FeeBalanceCalculator.CalculateNewBalance(-1m, 10m));
            AssertEx.Throws<ArgumentOutOfRangeException>(() => FeeBalanceCalculator.CalculateNewBalance(100m, 0m));
            AssertEx.Throws<ArgumentOutOfRangeException>(() => FeeBalanceCalculator.CalculateNewBalance(100m, -5m));
        }

        private static async Task PaymentService_RecordsCalculatedPaymentBalancesAsync()
        {
            var repository = new FakeFeeRepository { AddPaymentRecordResult = true };
            var service = new PaymentService(repository);
            var request = CreatePaymentRecordRequest();
            request.StudentId = "  501  ";
            request.StudentName = "  Ama Mensah  ";
            request.ClassId = "  BASIC 9  ";
            request.PaymentMode = "  Cash  ";
            request.BursarName = "  Mr Accounts  ";
            request.CurrentBalance = 1000m;
            request.AmountPaid = 250m;

            PaymentRecordResult result = await service.RecordPaymentAsync(request);

            AssertEx.True(result.Success, result.Message);
            AssertEx.Equal(750m, result.NewBalance);
            AssertEx.Equal(1, repository.AddPaymentRecordCallCount);
            AssertEx.Equal("501", repository.LastPaymentStudentId);
            AssertEx.Equal("BASIC 9", repository.LastPaymentClassId);
            AssertEx.Equal("Ama Mensah", repository.LastPaymentStudentName);
            AssertEx.Equal(250m, repository.LastPaymentAmountPaid);
            AssertEx.Equal(750m, repository.LastPaymentNewBalance);
            AssertEx.Equal("Cash", repository.LastPaymentMode);
            AssertEx.Equal("Mr Accounts", repository.LastPaymentBursarName);
            AssertEx.Equal(request.PaymentDate.Date, repository.LastPaymentDate);
        }

        private static async Task PaymentService_RecordsOverpaymentsAsZeroBalanceAsync()
        {
            var repository = new FakeFeeRepository { AddPaymentRecordResult = true };
            var service = new PaymentService(repository);
            var request = CreatePaymentRecordRequest();
            request.CurrentBalance = 100m;
            request.AmountPaid = 150m;

            PaymentRecordResult result = await service.RecordPaymentAsync(request);

            AssertEx.True(result.Success, result.Message);
            AssertEx.Equal(0m, result.NewBalance);
            AssertEx.Equal(0m, repository.LastPaymentNewBalance);
        }

        private static async Task PaymentService_ValidatesRequiredPaymentFieldsAsync()
        {
            var repository = new FakeFeeRepository { AddPaymentRecordResult = true };
            var service = new PaymentService(repository);

            PaymentRecordResult missingStudentId = await service.RecordPaymentAsync(CreatePaymentRecordRequest(studentId: ""));
            PaymentRecordResult missingName = await service.RecordPaymentAsync(CreatePaymentRecordRequest(studentName: ""));
            PaymentRecordResult missingClass = await service.RecordPaymentAsync(CreatePaymentRecordRequest(classId: ""));
            PaymentRecordResult missingBursar = await service.RecordPaymentAsync(CreatePaymentRecordRequest(bursarName: ""));
            PaymentRecordResult invalidAmount = await service.RecordPaymentAsync(CreatePaymentRecordRequest(amountPaid: 0m));

            AssertEx.False(missingStudentId.Success);
            AssertEx.Contains("Student ID is required", missingStudentId.Message);
            AssertEx.False(missingName.Success);
            AssertEx.Contains("Student name is required", missingName.Message);
            AssertEx.False(missingClass.Success);
            AssertEx.Contains("Class is required", missingClass.Message);
            AssertEx.False(missingBursar.Success);
            AssertEx.Contains("Bursar name is required", missingBursar.Message);
            AssertEx.False(invalidAmount.Success);
            AssertEx.Contains("Payment amount must be greater than zero", invalidAmount.Message);
            AssertEx.Equal(0, repository.AddPaymentRecordCallCount);
        }

        private static async Task PaymentService_ReportsRepositorySaveFailuresAsync()
        {
            var repository = new FakeFeeRepository { AddPaymentRecordResult = false };
            var service = new PaymentService(repository);

            PaymentRecordResult result = await service.RecordPaymentAsync(CreatePaymentRecordRequest());

            AssertEx.False(result.Success);
            AssertEx.Equal(750m, result.NewBalance);
            AssertEx.Contains("Could not save payment record", result.Message);
            AssertEx.Equal(1, repository.AddPaymentRecordCallCount);
        }

        private static async Task AdditionalFeeService_PreviewsAndPostsFlatFeesAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await SeedAdditionalFeeStudentsAsync(database.ConnectionString);

                var repository = new AdditionalFeeRepository(database.ConnectionString);
                var service = new AdditionalFeeService(repository);

                var create = await service.CreateDraftAsync(new AdditionalFee
                {
                    FeeName = "Examination Fee",
                    Description = "End of term examination charge",
                    AcademicYear = "2026",
                    TermName = "First Term",
                    AssignmentMode = AdditionalFeeAssignmentModes.Flat,
                    DefaultAmount = 50m,
                    IsCompulsory = true,
                    AllowsPartPayment = true,
                    CreatedBy = "accountant1"
                });

                AssertEx.True(create.Success, create.Message);

                var preview = await service.PreviewAsync(create.AdditionalFeeId);
                AssertEx.Equal(3, preview.StudentCount);
                AssertEx.Equal(150m, preview.ExpectedTotal);

                var submit = await service.SubmitForApprovalAsync(create.AdditionalFeeId, "accountant1");
                AssertEx.True(submit.Success, submit.Message);

                var selfApproval = await service.ApproveAsync(create.AdditionalFeeId, "accountant1");
                AssertEx.False(selfApproval.Success);
                AssertEx.Contains("cannot approve", selfApproval.Message);

                var approval = await service.ApproveAsync(create.AdditionalFeeId, "headmaster1");
                AssertEx.True(approval.Success, approval.Message);

                var fee = await repository.GetByIdAsync(create.AdditionalFeeId);
                AssertEx.Equal(AdditionalFeeStatuses.Active, fee.Status);

                using (var connection = new SqlConnection(database.ConnectionString))
                {
                    await connection.OpenAsync();
                    AssertEx.Equal(3, await ScalarIntAsync(connection, "SELECT COUNT(*) FROM AdditionalFeeStudentCharges WHERE AdditionalFeeId = @p0", create.AdditionalFeeId));
                    AssertEx.Equal(3, await ScalarIntAsync(connection, "SELECT COUNT(*) FROM fees WHERE FeeName = @p0", "Examination Fee"));
                    AssertEx.Equal(150m, await ScalarDecimalAsync(connection, "SELECT SUM(Balance) FROM payment_record WHERE payment_mode = @p0", "Additional Fee Posted"));
                }
            }
        }

        private static async Task AdditionalFeeService_TargetsClassAndDepartmentAmountsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await SeedAdditionalFeeStudentsAsync(database.ConnectionString);

                var service = new AdditionalFeeService(new AdditionalFeeRepository(database.ConnectionString));

                var classFee = await service.CreateDraftAsync(new AdditionalFee
                {
                    FeeName = "Sports Fee",
                    AcademicYear = "2026",
                    TermName = "First Term",
                    AssignmentMode = AdditionalFeeAssignmentModes.Class,
                    DefaultAmount = 0m,
                    IsCompulsory = true,
                    AllowsPartPayment = false,
                    CreatedBy = "accountant1",
                    Amounts =
                    {
                        new AdditionalFeeAmount { ScopeType = AdditionalFeeScopeTypes.Class, ScopeKey = "BASIC 1", Amount = 20m },
                        new AdditionalFeeAmount { ScopeType = AdditionalFeeScopeTypes.Class, ScopeKey = "BASIC 2", Amount = 30m }
                    }
                });

                AssertEx.True(classFee.Success, classFee.Message);
                var classPreview = await service.PreviewAsync(classFee.AdditionalFeeId);
                AssertEx.Equal(2, classPreview.StudentCount);
                AssertEx.Equal(50m, classPreview.ExpectedTotal);

                var departmentFee = await service.CreateDraftAsync(new AdditionalFee
                {
                    FeeName = "Development Levy",
                    AcademicYear = "2026",
                    TermName = "First Term",
                    AssignmentMode = AdditionalFeeAssignmentModes.Department,
                    DefaultAmount = 0m,
                    IsCompulsory = true,
                    AllowsPartPayment = true,
                    CreatedBy = "accountant1",
                    Amounts =
                    {
                        new AdditionalFeeAmount { ScopeType = AdditionalFeeScopeTypes.Department, ScopeKey = TimetableDepartments.LowerPrimary, Amount = 12m },
                        new AdditionalFeeAmount { ScopeType = AdditionalFeeScopeTypes.Department, ScopeKey = TimetableDepartments.JuniorHighSchool, Amount = 40m }
                    }
                });

                AssertEx.True(departmentFee.Success, departmentFee.Message);
                var departmentPreview = await service.PreviewAsync(departmentFee.AdditionalFeeId);
                AssertEx.Equal(3, departmentPreview.StudentCount);
                AssertEx.Equal(64m, departmentPreview.ExpectedTotal);
            }
        }

        private static async Task AdditionalFeeService_QueuesParentSmsOnApprovalAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await SeedAdditionalFeeStudentsAsync(database.ConnectionString);

                var repository = new AdditionalFeeRepository(database.ConnectionString);
                var service = new AdditionalFeeService(repository);

                var create = await service.CreateDraftAsync(new AdditionalFee
                {
                    FeeName = "PTA Development Levy",
                    Description = "Approved parent notification test",
                    AcademicYear = "2026",
                    TermName = "First Term",
                    AssignmentMode = AdditionalFeeAssignmentModes.Flat,
                    DefaultAmount = 15m,
                    IsCompulsory = true,
                    AllowsPartPayment = true,
                    NotifyParentsBySms = true,
                    CreatedBy = "accountant1"
                });

                AssertEx.True(create.Success, create.Message);
                var submit = await service.SubmitForApprovalAsync(create.AdditionalFeeId, "accountant1");
                AssertEx.True(submit.Success, submit.Message);
                var approval = await service.ApproveAsync(create.AdditionalFeeId, "headmaster1");
                AssertEx.True(approval.Success, approval.Message);

                using (var connection = new SqlConnection(database.ConnectionString))
                {
                    await connection.OpenAsync();
                    AssertEx.Equal(3, await ScalarIntAsync(connection,
                        "SELECT COUNT(*) FROM SmsOutbox WHERE SenderId = @p0",
                        SmsSenderIds.FeeReminder));
                    AssertEx.Equal(3, await ScalarIntAsync(connection,
                        "SELECT COUNT(*) FROM SmsOutbox WHERE Message LIKE '%' + @p0 + '%'",
                        "PTA Development Levy"));
                }
            }
        }

        private static async Task AdditionalFeeService_BlocksDuplicateOpenFeesAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var service = new AdditionalFeeService(new AdditionalFeeRepository(database.ConnectionString));
                var first = await service.CreateDraftAsync(new AdditionalFee
                {
                    FeeName = "PTA Levy",
                    AcademicYear = "2026",
                    TermName = "Second Term",
                    AssignmentMode = AdditionalFeeAssignmentModes.Flat,
                    DefaultAmount = 25m,
                    CreatedBy = "accountant1",
                    IsCompulsory = true,
                    AllowsPartPayment = true
                });

                var duplicate = await service.CreateDraftAsync(new AdditionalFee
                {
                    FeeName = "pta levy",
                    AcademicYear = "2026",
                    TermName = "second term",
                    AssignmentMode = AdditionalFeeAssignmentModes.Flat,
                    DefaultAmount = 25m,
                    CreatedBy = "accountant2",
                    IsCompulsory = true,
                    AllowsPartPayment = true
                });

                AssertEx.True(first.Success, first.Message);
                AssertEx.False(duplicate.Success);
                AssertEx.Contains("already exists", duplicate.Message);
            }
        }

        private static void StudentService_MapsTuitionFeesByClass()
        {
            var service = CreateStudentService();

            AssertEx.Equal(2000m, service.GetFeeForClass("CRECHE"));
            AssertEx.Equal(3450m, service.GetFeeForClass("NURSERY 1"));
            AssertEx.Equal(3750m, service.GetFeeForClass("NURSERY 2"));
            AssertEx.Equal(3654m, service.GetFeeForClass("KINDERGARTEN 1"));
            AssertEx.Equal(2423m, service.GetFeeForClass("KINDERGARTEN 2"));
            AssertEx.Equal(2423m, service.GetFeeForClass("BASIC 1"));
            AssertEx.Equal(2423m, service.GetFeeForClass("BASIC 9"));
            AssertEx.Equal(2423m, service.GetFeeForClass(" basic 4 "));
            AssertEx.Equal(1200m, service.GetFeeForClass("UNKNOWN"));
            AssertEx.Equal(1200m, service.GetFeeForClass(null));
        }

        private static void StudentService_MapsEveryConfiguredClass()
        {
            var service = CreateStudentService();

            foreach (string className in AppConfig.ClassNames)
            {
                decimal fee = service.GetFeeForClass(className);
                AssertEx.True(fee > 0m, className + " should map to a positive fee.");
                AssertEx.NotEqual(1200m, fee, className + " should not fall through to the unknown-class default fee.");
            }
        }

        private static async Task StudentService_CreatesOpeningFeeRecordsOnAddAsync()
        {
            var studentRepo = new FakeStudentRepository { AddResult = true, GeneratedStudentId = "501" };
            var feeRepo = new FakeFeeRepository();
            var service = CreateStudentService(studentRepo, feeRepo);
            var student = CreateValidStudent("BASIC 9");
            student.StudentID = "";

            var result = await service.AddStudentAsync(student);

            AssertEx.True(result.Success, result.Message);
            AssertEx.Equal(1, studentRepo.AddCallCount);
            AssertEx.Equal("501", student.StudentID);
            AssertEx.Equal(1, feeRepo.AddInitialFeeRecordCallCount);
            AssertEx.Equal(1, feeRepo.AddInitialPaymentRecordCallCount);
            AssertEx.Equal("501", feeRepo.LastInitialFeeStudentId);
            AssertEx.Equal("BASIC 9", feeRepo.LastInitialFeeClassId);
            AssertEx.Equal(2423m, feeRepo.LastInitialFeeAmount);
            AssertEx.Equal(2423m, feeRepo.LastInitialPaymentBalance);
        }

        private static async Task StudentService_RejectsInvalidStudentsBeforePersistenceAsync()
        {
            var studentRepo = new FakeStudentRepository { AddResult = true };
            var feeRepo = new FakeFeeRepository();
            var service = CreateStudentService(studentRepo, feeRepo);
            var student = CreateValidStudent("BASIC 1");
            student.FirstName = "Ama1";

            var result = await service.AddStudentAsync(student);

            AssertEx.False(result.Success);
            AssertEx.Contains("First name contains invalid characters", result.Message);
            AssertEx.Equal(0, studentRepo.AddCallCount);
            AssertEx.Equal(0, feeRepo.AddInitialFeeRecordCallCount);
            AssertEx.Equal(0, feeRepo.AddInitialPaymentRecordCallCount);
        }

        private static async Task StudentService_RejectsDuplicateStudentIdsBeforeFeeCreationAsync()
        {
            var studentRepo = new FakeStudentRepository { ExistsResult = true, AddResult = true };
            var feeRepo = new FakeFeeRepository();
            var service = CreateStudentService(studentRepo, feeRepo);
            var student = CreateValidStudent("BASIC 1");
            student.StudentID = "123";

            var result = await service.AddStudentAsync(student);

            AssertEx.False(result.Success);
            AssertEx.Contains("already exists", result.Message);
            AssertEx.Equal(0, studentRepo.AddCallCount);
            AssertEx.Equal(0, feeRepo.AddInitialFeeRecordCallCount);
            AssertEx.Equal(0, feeRepo.AddInitialPaymentRecordCallCount);
        }

        private static async Task StudentService_UpdatesFeeRecordsOnClassChangeAsync()
        {
            var studentRepo = new FakeStudentRepository { ExistsResult = true, UpdateResult = true };
            var feeRepo = new FakeFeeRepository();
            var service = CreateStudentService(studentRepo, feeRepo);
            var student = CreateValidStudent("NURSERY 2");
            student.StudentID = "777";

            var result = await service.UpdateStudentAsync(student);

            AssertEx.True(result.Success, result.Message);
            AssertEx.Equal(1, studentRepo.UpdateCallCount);
            AssertEx.Equal(1, feeRepo.UpdateFeeRecordCallCount);
            AssertEx.Equal(1, feeRepo.UpdatePaymentRecordCallCount);
            AssertEx.Equal("777", feeRepo.LastUpdatedFeeStudentId);
            AssertEx.Equal("NURSERY 2", feeRepo.LastUpdatedFeeClassId);
            AssertEx.Equal(3750m, feeRepo.LastUpdatedFeeAmount);
            AssertEx.Equal(3750m, feeRepo.LastUpdatedPaymentBalance);
        }

        private static async Task StudentService_PromotesSelectedStudentsAndRefreshesFeeRecordsAsync()
        {
            var studentRepo = new FakeStudentRepository { BatchUpdateResult = true };
            var feeRepo = new FakeFeeRepository();
            var service = CreateStudentService(studentRepo, feeRepo);

            var ama = CreateValidStudent("BASIC 1");
            ama.StudentID = "9010";
            ama.FirstName = "Ama";
            ama.LastName = "Mensah";

            var yaw = CreateValidStudent("BASIC 1");
            yaw.StudentID = "9011";
            yaw.FirstName = "Yaw";
            yaw.LastName = "Boateng";

            studentRepo.StudentsById["9010"] = ama;
            studentRepo.StudentsById["9011"] = yaw;

            var result = await service.PromoteStudentsAsync(new[] { " 9010 ", "9011", "9010", "" }, "BASIC 8");

            AssertEx.True(result.Success, result.Message);
            AssertEx.Contains("2 student(s) promoted to BASIC 8", result.Message);
            AssertEx.Equal(1, studentRepo.BatchUpdateCallCount);
            AssertEx.Equal(2, studentRepo.LastBatchStudentIds.Count);
            AssertEx.Equal("9010", studentRepo.LastBatchStudentIds[0]);
            AssertEx.Equal("9011", studentRepo.LastBatchStudentIds[1]);
            AssertEx.Equal("BASIC 8", studentRepo.LastBatchClassId);
            AssertEx.Equal(2, feeRepo.UpdateFeeRecordCallCount);
            AssertEx.Equal(2, feeRepo.UpdatePaymentRecordCallCount);
            AssertEx.Equal("9011", feeRepo.LastUpdatedPaymentStudentId);
            AssertEx.Equal("BASIC 8", feeRepo.LastUpdatedPaymentClassId);
            AssertEx.Equal("Yaw Boateng", feeRepo.LastUpdatedPaymentStudentName);
            AssertEx.Equal(2423m, feeRepo.LastUpdatedPaymentBalance);
        }

        private static async Task DraftAdmissionService_ApprovesDraftIntoStudentAndFeeRecordsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var drafts = new DraftAdmissionRepository(database.ConnectionString);
                var feeRepo = new FeeRepository(database.ConnectionString);
                var studentService = new StudentService(new StudentRepository(database.ConnectionString), feeRepo);
                var service = new DraftAdmissionService(drafts, studentService, feeRepo);
                var draft = CreateValidDraftAdmission();

                var create = await service.CreateDraftAsync(draft);
                AssertEx.True(create.Ok, create.Message);

                var pending = await service.GetPendingAsync();
                var pendingDraft = pending.Single();

                var approval = await service.ApproveAsync(pendingDraft.DraftID, "Accounts Officer");

                AssertEx.True(approval.Ok, approval.Message);
                AssertEx.NotNull(approval.Student);
                AssertEx.False(string.IsNullOrWhiteSpace(approval.Student.StudentID));
                AssertEx.Equal("0241234567", approval.Student.EmergencyContact);

                AssertEx.Equal(0, await service.GetPendingCountAsync());
                var savedStudent = await new StudentRepository(database.ConnectionString).GetByIdAsync(approval.Student.StudentID);
                AssertEx.NotNull(savedStudent);
                AssertEx.Equal("BASIC 1", savedStudent.ClassID);

                decimal? latestBalance = await feeRepo.GetLatestBalanceAsync(approval.Student.StudentID);
                AssertEx.Equal(500m, latestBalance.Value);

                var ledger = await feeRepo.GetStudentPaymentHistoryTableAsync(approval.Student.StudentID);
                AssertEx.Equal(3, ledger.Rows.Count);
            }
        }

        private static async Task SmsService_QueuesAdmissionSmsWithoutLiveKeysAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                string previousConnection = Environment.GetEnvironmentVariable("NYANSAPO_CONNECTION_STRING");
                try
                {
                    Environment.SetEnvironmentVariable("NYANSAPO_CONNECTION_STRING", database.ConnectionString);

                    var student = CreateValidStudent("BASIC 1");
                    student.StudentID = "9001";
                    student.EmergencyContact = "0241234567";
                    student.GuardianName = "Parent Test";

                    var result = await SmsService.SendStudentAdmissionAsync(
                        student.EmergencyContact,
                        student,
                        admissionFeePaid: 100m,
                        schoolFeePaid: 500m,
                        termTotal: 1000m);

                    AssertEx.True(result.Success, result.Message);
                    AssertEx.False(SmsService.IsLive);

                    var log = await new SmsOutboxRepository(database.ConnectionString).GetLogTableAsync();
                    AssertEx.Equal(1, log.Rows.Count);
                    AssertEx.Equal("233241234567", log.Rows[0]["Recipient"].ToString());
                    AssertEx.Equal(SmsSenderIds.StudentAdmission, log.Rows[0]["From"].ToString());
                    AssertEx.Contains("Admission fee paid: GHS 100.00", log.Rows[0]["Message"].ToString());
                    AssertEx.Contains("School fee paid: GHS 500.00 out of GHS 1,000.00", log.Rows[0]["Message"].ToString());
                }
                finally
                {
                    Environment.SetEnvironmentVariable("NYANSAPO_CONNECTION_STRING", previousConnection);
                }
            }
        }

        private static void LeaveRequest_CalculatesInclusiveDuration()
        {
            var request = CreateLeaveRequest(new DateTime(2026, 6, 3), new DateTime(2026, 6, 3));
            AssertEx.Equal(1, request.DurationDays);

            request.EndDate = new DateTime(2026, 6, 7);
            AssertEx.Equal(5, request.DurationDays);
        }

        private static void LeaveTermCalendar_MapsSchoolTerms()
        {
            var term1 = AppConfig.Leave.GetTerm(new DateTime(2026, 9, 1));
            AssertEx.Equal("Term 1 2026/2027", term1.TermName);
            AssertEx.Equal(new DateTime(2026, 9, 1), term1.Start);
            AssertEx.Equal(new DateTime(2026, 12, 31), term1.End);

            var term2 = AppConfig.Leave.GetTerm(new DateTime(2026, 1, 15));
            AssertEx.Equal("Term 2 2025/2026", term2.TermName);
            AssertEx.Equal(new DateTime(2026, 1, 1), term2.Start);
            AssertEx.Equal(new DateTime(2026, 4, 30), term2.End);

            var term3 = AppConfig.Leave.GetTerm(new DateTime(2026, 6, 3));
            AssertEx.Equal("Term 3 2025/2026", term3.TermName);
            AssertEx.Equal(new DateTime(2026, 5, 1), term3.Start);
            AssertEx.Equal(new DateTime(2026, 8, 31), term3.End);
        }

        private static async Task LeaveService_RejectsInvalidLeaveRangesAsync()
        {
            var repository = new FakeLeaveRepository { AddResult = true };
            var service = new LeaveService(repository);
            var request = CreateLeaveRequest(new DateTime(2026, 6, 7), new DateTime(2026, 6, 3));

            var result = await service.ApplyForLeaveAsync(request);

            AssertEx.False(result.Success);
            AssertEx.Contains("End date cannot be before start date", result.Message);
            AssertEx.Equal(0, repository.HasApprovedOverlapCallCount);
            AssertEx.Equal(0, repository.AddLeaveRequestCallCount);
        }

        private static async Task LeaveService_RejectsOverlappingApprovedLeaveAsync()
        {
            var repository = new FakeLeaveRepository { AddResult = true, HasApprovedOverlapResult = true };
            var service = new LeaveService(repository);
            var request = CreateLeaveRequest(new DateTime(2026, 6, 3), new DateTime(2026, 6, 7));

            var result = await service.ApplyForLeaveAsync(request);

            AssertEx.False(result.Success);
            AssertEx.Contains("overlaps", result.Message);
            AssertEx.Equal(1, repository.HasApprovedOverlapCallCount);
            AssertEx.Equal("EMP001", repository.LastOverlapEmployeeId);
            AssertEx.Equal(new DateTime(2026, 6, 3), repository.LastOverlapStartDate);
            AssertEx.Equal(new DateTime(2026, 6, 7), repository.LastOverlapEndDate);
            AssertEx.Equal(0, repository.AddLeaveRequestCallCount);
        }

        private static async Task LeaveService_SubmitsPendingLeaveRequestsAsync()
        {
            var repository = new FakeLeaveRepository { AddResult = true };
            var service = new LeaveService(repository);
            var request = CreateLeaveRequest(new DateTime(2026, 6, 3), new DateTime(2026, 6, 7));

            var result = await service.ApplyForLeaveAsync(request);

            AssertEx.True(result.Success, result.Message);
            AssertEx.Equal("PENDING", request.Status);
            AssertEx.Equal(1, repository.HasApprovedOverlapCallCount);
            AssertEx.Equal(1, repository.AddLeaveRequestCallCount);
            AssertEx.Equal(request, repository.LastAddedLeaveRequest);
        }

        private static async Task LeaveService_CalculatesTermLeaveBalanceAsync()
        {
            var repository = new FakeLeaveRepository { ApprovedDaysInRangeResult = 4 };
            var service = new LeaveService(repository);
            var expectedTerm = AppConfig.Leave.CurrentTerm;

            LeaveBalance balance = await service.GetLeaveBalanceAsync("EMP001", "Kofi Boateng");

            AssertEx.Equal("EMP001", balance.EmployeeID);
            AssertEx.Equal("Kofi Boateng", balance.EmployeeName);
            AssertEx.Equal(expectedTerm.TermName, balance.TermName);
            AssertEx.Equal(expectedTerm.Start, balance.TermStart);
            AssertEx.Equal(expectedTerm.End, balance.TermEnd);
            AssertEx.Equal(AppConfig.Leave.DaysPerTerm, balance.Entitlement);
            AssertEx.Equal(4, balance.DaysUsed);
            AssertEx.Equal(Math.Max(0, AppConfig.Leave.DaysPerTerm - 4), balance.Remaining);
            AssertEx.Equal(expectedTerm.Start, repository.LastApprovedDaysTermStart);
            AssertEx.Equal(expectedTerm.End, repository.LastApprovedDaysTermEnd);
        }

        private static async Task LeaveService_FiltersRequestTablesByStatusAsync()
        {
            var repository = new FakeLeaveRepository();
            var service = new LeaveService(repository);

            await service.GetLeaveRequestsTableAsync();
            await service.GetLeaveRequestsTableAsync("ALL");
            await service.GetLeaveRequestsTableAsync("PENDING");

            AssertEx.Equal(2, repository.GetAllLeaveRequestsTableCallCount);
            AssertEx.Equal(1, repository.GetLeaveRequestsByStatusCallCount);
            AssertEx.Equal("PENDING", repository.LastStatusFilter);
        }

        private static void ExamResult_CalculatesTotalScoreGradeAndRemark()
        {
            var result = CreateExamResult(category1: 32m, category2: 8m, category3: 8m, examScore: 40m);

            result.Calculate();

            AssertEx.Equal(80m, result.TotalScore);
            AssertEx.Equal("1", result.Grade);
            AssertEx.Equal("Advance", result.Remark);
        }

        private static void ExamResult_MapsGradeBoundaries()
        {
            AssertExamGrade(80m, "1", "Advance");
            AssertExamGrade(79.99m, "2", "Proficiency");
            AssertExamGrade(75m, "2", "Proficiency");
            AssertExamGrade(74.99m, "3", "Approaching Proficiency");
            AssertExamGrade(70m, "3", "Approaching Proficiency");
            AssertExamGrade(69.99m, "4", "Developing");
            AssertExamGrade(65m, "4", "Developing");
            AssertExamGrade(64.99m, "5", "Beginning");
        }

        private static async Task ExamService_AddsNewCalculatedResultsAsync()
        {
            var repository = new FakeExamRepository
            {
                ResultExists = false,
                AddResult = true,
                UpdateResult = true
            };
            var service = new ExamService(repository);
            var result = CreateExamResult(category1: 24m, category2: 6m, category3: 6m, examScore: 35m);

            var saveResult = await service.SaveResultsAsync(new[] { result });

            AssertEx.True(saveResult.Success, saveResult.Message);
            AssertEx.Contains("1 result(s) saved", saveResult.Message);
            AssertEx.Equal(1, repository.ResultExistsCallCount);
            AssertEx.Equal(1, repository.AddResultCallCount);
            AssertEx.Equal(0, repository.UpdateResultCallCount);
            AssertEx.Equal(65m, repository.LastAddedResult.TotalScore);
            AssertEx.Equal("4", repository.LastAddedResult.Grade);
            AssertEx.Equal("Developing", repository.LastAddedResult.Remark);
        }

        private static async Task ExamService_UpdatesExistingCalculatedResultsAsync()
        {
            var repository = new FakeExamRepository
            {
                ResultExists = true,
                AddResult = true,
                UpdateResult = true
            };
            var service = new ExamService(repository);
            var result = CreateExamResult(category1: 30m, category2: 9m, category3: 9m, examScore: 45m);

            var saveResult = await service.SaveResultsAsync(new[] { result });

            AssertEx.True(saveResult.Success, saveResult.Message);
            AssertEx.Equal(1, repository.ResultExistsCallCount);
            AssertEx.Equal(0, repository.AddResultCallCount);
            AssertEx.Equal(1, repository.UpdateResultCallCount);
            AssertEx.Equal(85m, repository.LastUpdatedResult.TotalScore);
            AssertEx.Equal("1", repository.LastUpdatedResult.Grade);
            AssertEx.Equal("Advance(A)", repository.LastUpdatedResult.Remark);
        }

        private static async Task ExamService_ReportsRepositorySaveErrorsAsync()
        {
            var repository = new FakeExamRepository { ThrowOnResultExists = true };
            var service = new ExamService(repository);

            var result = await service.SaveResultsAsync(new[] { CreateExamResult() });

            AssertEx.False(result.Success);
            AssertEx.Contains("Error saving results", result.Message);
            AssertEx.Contains("repository unavailable", result.Message);
        }

        private static async Task ReportCardPDFGenerator_CreatesValidPdfAsync()
        {
            var generator = new ReportCardPDFGenerator();
            var data = new ReportCardData
            {
                StudentID = "9001",
                StudentName = "Ama Test",
                ClassID = "BASIC 1",
                Gender = "FEMALE",
                AdmissionDate = new DateTime(2024, 9, 1),
                Term = "TERM 1",
                Year = "2026",
                PresentDays = 58,
                TotalSchoolDays = 60,
                OverallPosition = 1,
                TotalStudentsInClass = 15,
                SchoolInfo = new SchoolInfo
                {
                    Name = "KINGDOM PREPARATORY SCHOOL",
                    Location = "AKIM ODA-ABENASE",
                    PhoneNumbers = "0548050141 / 0246087609"
                },
                Remarks = new StudentTermRemarks
                {
                    Attitude = "Excellent",
                    Interest = "Good",
                    Conduct = "Excellent",
                    ClassTeacherRemarks = "Shows steady academic progress.",
                    HeadTeacherRemarks = "Promoted."
                },
                SubjectResults = new List<SubjectResult>
                {
                    new SubjectResult
                    {
                        Subject = "ENGLISH LANGUAGE",
                        ClassScore = 45m,
                        ExamScore = 80m,
                        TotalScore = 76m,
                        Grade = "2",
                        Remark = "Proficiency",
                        PositionInClass = 2
                    },
                    new SubjectResult
                    {
                        Subject = "MATHEMATICS",
                        ClassScore = 48m,
                        ExamScore = 86m,
                        TotalScore = 81m,
                        Grade = "1",
                        Remark = "Advance",
                        PositionInClass = 1
                    }
                }
            };

            var bytes = await generator.GeneratePDFAsync(data);

            AssertEx.NotNull(bytes);
            AssertEx.True(bytes.Length > 1000, "Generated report card PDF should not be empty.");
            AssertEx.Equal((byte)'%', bytes[0]);
            AssertEx.Equal((byte)'P', bytes[1]);
            AssertEx.Equal((byte)'D', bytes[2]);
            AssertEx.Equal((byte)'F', bytes[3]);
        }

        private static void AuthService_HashesAndVerifiesPasswords()
        {
            const string password = "Secure123";
            string hash = AuthService.HashPassword(password);

            AssertEx.NotNull(hash);
            AssertEx.NotEqual(password, hash);
            AssertEx.True(AuthService.IsHashedPassword(hash));
            AssertEx.True(AuthService.VerifyPassword(password, hash));
            AssertEx.False(AuthService.VerifyPassword("Wrong123", hash));
        }

        private static void AuthService_RejectsInvalidLoginCredentials()
        {
            AssertEx.Contains("Username is required", AuthService.ValidateLoginCredentials("", "Password123"));
            AssertEx.Contains("Password is required", AuthService.ValidateLoginCredentials("admin", ""));
            AssertEx.Contains("Invalid username format", AuthService.ValidateLoginCredentials("ab", "Password123"));
            AssertEx.Empty(AuthService.ValidateLoginCredentials("admin", "Password123"));
        }

        private static void AuthService_ValidatesRegistrationInput()
        {
            AssertEx.Empty(AuthService.ValidateRegistration("teacher_1", "Password123", "Password123", "TEACHER"));
            AssertEx.Contains("Username cannot be empty", AuthService.ValidateRegistration("", "Password123", "Password123", "TEACHER"));
            AssertEx.Contains("Password must be", AuthService.ValidateRegistration("teacher_1", "weak", "weak", "TEACHER"));
            AssertEx.Contains("Passwords do not match", AuthService.ValidateRegistration("teacher_1", "Password123", "Password124", "TEACHER"));
            AssertEx.Contains("Please select", AuthService.ValidateRegistration("teacher_1", "Password123", "Password123", "--- Select ---"));
        }

        private static async Task AuthService_ExplainsMissingLoginAccountsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                string previousConnection = Environment.GetEnvironmentVariable("NYANSAPO_CONNECTION_STRING");
                try
                {
                    Environment.SetEnvironmentVariable("NYANSAPO_CONNECTION_STRING", database.ConnectionString);
                    await AuthService.EnsureDatabaseSetupAsync();
                    var register = await AuthService.RegisterAsync("admin", "Password123", "Password123", "ADMINISTRATOR");
                    AssertEx.True(register.Success, register.Message);

                    var missing = await AuthService.LoginAsync("suma", "Password123");

                    AssertEx.False(missing.Success);
                    AssertEx.Contains("No account was found for username 'suma'", missing.Message);
                    AssertEx.Contains("admin", missing.Message);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("NYANSAPO_CONNECTION_STRING", previousConnection);
                    AuthService.Logout();
                }
            }
        }

        private static void AuthService_BlocksProtectedScreensWhileLoggedOut()
        {
            AuthService.Logout();
            AssertEx.False(AuthService.CanAccess("frmDashboard"));
            AssertEx.False(AuthService.CanAccess("frmTeacherDashboard"));
            AssertEx.False(AuthService.CanAccess(null));
            AssertEx.False(AuthService.CanAccess(""));
            AssertEx.False(AuthService.CanAccess("UnregisteredForm"));
        }

        private static void AuthService_DeniesBlankAndUnknownScreenKeys()
        {
            SetCurrentUser("director", AuthService.UserRole.Director);

            AssertEx.False(AuthService.CanAccess(null));
            AssertEx.False(AuthService.CanAccess(""));
            AssertEx.False(AuthService.CanAccess("   "));
            AssertEx.False(AuthService.CanAccess("UnregisteredForm"));

            AuthService.Logout();
        }

        private static void AuthService_EnforcesRoleSpecificScreenAccess()
        {
            SetCurrentUser("director", AuthService.UserRole.Director);
            AssertEx.True(AuthService.CanAccess("frmDashboard"));
            AssertEx.True(AuthService.CanAccess("frmEmployee"));
            AssertEx.True(AuthService.CanAccess("frmAdminDashboard"));
            AssertEx.True(AuthService.CanAccess("frmSyncSettings"));
            AssertEx.True(AuthService.CanWrite("Finance.Expense.Manage"));
            AssertEx.True(AuthService.CanWrite("Finance.Scholarship.Approve"));
            AssertEx.True(AuthService.CanWrite("Settings.SchoolProfile.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.GradingScheme.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.Subjects.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.ExamSetup.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.Email.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.Sync.Manage"));
            AssertEx.True(AuthService.CanWrite("Students.Register"));
            AssertEx.True(AuthService.CanWrite("Students.Edit"));
            AssertEx.True(AuthService.CanWrite("Students.Import"));
            AssertEx.True(AuthService.CanWrite("Students.Promote"));
            AssertEx.True(AuthService.CanWrite("Students.RollOut"));
            AssertEx.True(AuthService.CanWrite("Students.TransportAssignment"));
            AssertEx.True(AuthService.CanWrite("Staff.Register"));
            AssertEx.True(AuthService.CanWrite("Staff.Edit"));
            AssertEx.True(AuthService.CanWrite("Staff.Delete"));
            AssertEx.True(AuthService.CanWrite("Staff.Terminate"));
            AssertEx.True(AuthService.CanWrite("Leave.Submit"));
            AssertEx.True(AuthService.CanWrite("Leave.Approve"));
            AssertEx.True(AuthService.CanWrite("Academics.ExamResults.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Attendance.Record"));
            AssertEx.True(AuthService.CanWrite("Academics.ClassStructure.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Calendar.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Session.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Timetable.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Archive.Restore"));
            AssertEx.True(AuthService.CanWrite("Admin.Backup.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Notice.Send"));
            AssertEx.True(AuthService.CanWrite("Admin.Sync.Run"));
            AssertEx.False(AuthService.CanWrite("Finance.FeePayment.Record"));
            AssertEx.False(AuthService.CanAccess("frmTeacherDashboard"));

            SetCurrentUser("admin", AuthService.UserRole.Administrator);
            AssertEx.True(AuthService.CanWrite("Settings.SchoolProfile.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.GradingScheme.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.Subjects.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.ExamSetup.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.Email.Manage"));
            AssertEx.True(AuthService.CanWrite("Settings.Sync.Manage"));
            AssertEx.True(AuthService.CanWrite("Students.Register"));
            AssertEx.True(AuthService.CanWrite("Students.Edit"));
            AssertEx.True(AuthService.CanWrite("Students.Import"));
            AssertEx.True(AuthService.CanWrite("Students.Promote"));
            AssertEx.True(AuthService.CanWrite("Students.RollOut"));
            AssertEx.True(AuthService.CanWrite("Students.TransportAssignment"));
            AssertEx.True(AuthService.CanWrite("Staff.Register"));
            AssertEx.True(AuthService.CanWrite("Staff.Edit"));
            AssertEx.True(AuthService.CanWrite("Staff.Delete"));
            AssertEx.True(AuthService.CanWrite("Staff.Terminate"));
            AssertEx.True(AuthService.CanWrite("Leave.Submit"));
            AssertEx.True(AuthService.CanWrite("Leave.Approve"));
            AssertEx.True(AuthService.CanWrite("Academics.ExamResults.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Attendance.Record"));
            AssertEx.True(AuthService.CanWrite("Academics.ClassStructure.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Calendar.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Session.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Timetable.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Archive.Restore"));
            AssertEx.True(AuthService.CanWrite("Admin.Backup.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Notice.Send"));
            AssertEx.True(AuthService.CanWrite("Admin.Sync.Run"));
            AssertEx.False(AuthService.CanWrite("Finance.FeePayment.Record"));

            SetCurrentUser("headmaster", AuthService.UserRole.Headmaster);
            AssertEx.True(AuthService.CanAccess("frmDashboard"));
            AssertEx.True(AuthService.CanAccess("frmDashboardCharts"));
            AssertEx.True(AuthService.CanAccess("frmAddStd"));
            AssertEx.True(AuthService.CanWrite("Students.Promote"));
            AssertEx.True(AuthService.CanWrite("Leave.Approve"));

            // Explicit denials we implemented for Headmaster
            AssertEx.False(AuthService.CanAccess("frmOutstandingFees"));
            AssertEx.False(AuthService.CanAccess("frmFess"));
            AssertEx.False(AuthService.CanAccess("frmFessPayment"));
            AssertEx.False(AuthService.CanAccess("frmPaymentHistory"));
            AssertEx.False(AuthService.CanAccess("frmSyncSettings"));
            AssertEx.False(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Backup.Manage"));

            SetCurrentUser("teacher", AuthService.UserRole.Teacher, 12);
            AssertEx.True(AuthService.CanAccess("frmTeacherDashboard"));
            AssertEx.True(AuthService.CanAccess("EXAMS"));
            AssertEx.True(AuthService.CanAccess("frmNotice"));
            AssertEx.False(AuthService.CanAccess("frmEmployee"));
            AssertEx.False(AuthService.CanAccess("frmClassManager"));
            AssertEx.False(AuthService.CanAccess("frmSyncSettings"));
            AssertEx.False(AuthService.CanWrite("Finance.FeePayment.Record"));
            AssertEx.False(AuthService.CanWrite("Finance.Expense.Manage"));
            AssertEx.False(AuthService.CanWrite("Students.Register"));
            AssertEx.False(AuthService.CanWrite("Students.Edit"));
            AssertEx.False(AuthService.CanWrite("Students.Import"));
            AssertEx.False(AuthService.CanWrite("Students.Promote"));
            AssertEx.False(AuthService.CanWrite("Students.RollOut"));
            AssertEx.False(AuthService.CanWrite("Students.TransportAssignment"));
            AssertEx.False(AuthService.CanWrite("Staff.Register"));
            AssertEx.False(AuthService.CanWrite("Staff.Edit"));
            AssertEx.False(AuthService.CanWrite("Staff.Delete"));
            AssertEx.False(AuthService.CanWrite("Staff.Terminate"));
            AssertEx.True(AuthService.CanWrite("Leave.Submit"));
            AssertEx.False(AuthService.CanWrite("Leave.Approve"));
            AssertEx.True(AuthService.CanWrite("Academics.ExamResults.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Attendance.Record"));
            AssertEx.False(AuthService.CanWrite("Academics.ClassStructure.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Calendar.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Session.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Timetable.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Archive.Restore"));
            AssertEx.False(AuthService.CanWrite("Admin.Backup.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Notice.Send"));
            AssertEx.False(AuthService.CanWrite("Admin.Sync.Run"));

            SetCurrentUser("accountant", AuthService.UserRole.Accountant);
            AssertEx.True(AuthService.CanAccess("frmFessPayment"));
            AssertEx.True(AuthService.CanAccess("frmScholarships"));
            AssertEx.True(AuthService.CanWrite("Finance.FeePayment.Record"));
            AssertEx.True(AuthService.CanWrite("Finance.TransportPayment.Record"));
            AssertEx.True(AuthService.CanWrite("Finance.Expense.Manage"));
            AssertEx.True(AuthService.CanWrite("Finance.Scholarship.Manage"));
            AssertEx.False(AuthService.CanWrite("Finance.Scholarship.Approve"));
            AssertEx.False(AuthService.CanWrite("Students.Register"));
            AssertEx.False(AuthService.CanWrite("Students.Edit"));
            AssertEx.False(AuthService.CanWrite("Students.Import"));
            AssertEx.False(AuthService.CanWrite("Students.Promote"));
            AssertEx.False(AuthService.CanWrite("Students.RollOut"));
            AssertEx.False(AuthService.CanWrite("Students.TransportAssignment"));
            AssertEx.False(AuthService.CanWrite("Staff.Register"));
            AssertEx.False(AuthService.CanWrite("Staff.Edit"));
            AssertEx.False(AuthService.CanWrite("Staff.Delete"));
            AssertEx.False(AuthService.CanWrite("Staff.Terminate"));
            AssertEx.True(AuthService.CanWrite("Leave.Submit"));
            AssertEx.False(AuthService.CanWrite("Leave.Approve"));
            AssertEx.False(AuthService.CanWrite("Academics.ExamResults.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Attendance.Record"));
            AssertEx.False(AuthService.CanWrite("Academics.ClassStructure.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Calendar.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Session.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Timetable.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Archive.Restore"));
            AssertEx.False(AuthService.CanWrite("Admin.Backup.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Notice.Send"));
            AssertEx.False(AuthService.CanWrite("Admin.Sync.Run"));
            AssertEx.False(AuthService.CanAccess("frmEmployee"));
            AssertEx.False(AuthService.CanAccess("frmClassManager"));
            AssertEx.False(AuthService.CanAccess("frmSyncStatus"));

            SetCurrentUser("headmaster", AuthService.UserRole.Headmaster);
            AssertEx.True(AuthService.CanAccess("frmClassManager"));
            AssertEx.True(AuthService.CanAccess("frmAcademicSessionManager"));
            AssertEx.True(AuthService.CanAccess("frmAcademicCalendar"));
            AssertEx.True(AuthService.CanAccess("frmTimetable"));
            AssertEx.False(AuthService.CanAccess("frmDatabaseCoverageAudit"));
            AssertEx.False(AuthService.CanAccess("frmFessPayment"));
            AssertEx.False(AuthService.CanWrite("Finance.TransportPayment.Record"));
            AssertEx.False(AuthService.CanWrite("Finance.Expense.Manage"));
            AssertEx.True(AuthService.CanWrite("Students.Register"));
            AssertEx.True(AuthService.CanWrite("Students.Edit"));
            AssertEx.True(AuthService.CanWrite("Students.Import"));
            AssertEx.True(AuthService.CanWrite("Students.Promote"));
            AssertEx.True(AuthService.CanWrite("Students.RollOut"));
            AssertEx.True(AuthService.CanWrite("Students.TransportAssignment"));
            AssertEx.False(AuthService.CanWrite("Staff.Register"));
            AssertEx.False(AuthService.CanWrite("Staff.Edit"));
            AssertEx.False(AuthService.CanWrite("Staff.Delete"));
            AssertEx.False(AuthService.CanWrite("Staff.Terminate"));
            AssertEx.True(AuthService.CanWrite("Leave.Submit"));
            AssertEx.True(AuthService.CanWrite("Leave.Approve"));
            AssertEx.True(AuthService.CanWrite("Academics.ExamResults.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Attendance.Record"));
            AssertEx.True(AuthService.CanWrite("Academics.ClassStructure.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Calendar.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Session.Manage"));
            AssertEx.True(AuthService.CanWrite("Academics.Timetable.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Archive.Restore"));
            AssertEx.False(AuthService.CanWrite("Admin.Backup.Manage"));
            AssertEx.True(AuthService.CanWrite("Admin.Notice.Send"));
            AssertEx.False(AuthService.CanWrite("Admin.Sync.Run"));
            AssertEx.False(AuthService.CanWrite("Settings.SchoolProfile.Manage"));
            AssertEx.False(AuthService.CanWrite("Settings.GradingScheme.Manage"));
            AssertEx.False(AuthService.CanWrite("Settings.Subjects.Manage"));
            AssertEx.False(AuthService.CanWrite("Settings.ExamSetup.Manage"));
            AssertEx.False(AuthService.CanWrite("Settings.Email.Manage"));
            AssertEx.False(AuthService.CanWrite("Settings.Sync.Manage"));

            SetCurrentUser("parent", AuthService.UserRole.Parent);
            AssertEx.False(AuthService.CanAccess("frmDashboard"));
            AssertEx.False(AuthService.CanWrite("Finance.FeePayment.Record"));
            AssertEx.False(AuthService.CanWrite("Students.Register"));
            AssertEx.False(AuthService.CanWrite("Staff.Register"));
            AssertEx.False(AuthService.CanWrite("Leave.Submit"));
            AssertEx.False(AuthService.CanWrite("Leave.Approve"));
            AssertEx.False(AuthService.CanWrite("Academics.ExamResults.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Attendance.Record"));
            AssertEx.False(AuthService.CanWrite("Academics.ClassStructure.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Calendar.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Session.Manage"));
            AssertEx.False(AuthService.CanWrite("Academics.Timetable.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Users.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Archive.Restore"));
            AssertEx.False(AuthService.CanWrite("Admin.Backup.Manage"));
            AssertEx.False(AuthService.CanWrite("Admin.Notice.Send"));
            AssertEx.False(AuthService.CanWrite("Admin.Sync.Run"));
            AssertEx.False(AuthService.CanWrite("Unknown.Action"));

            AuthService.Logout();
        }

        private static void ValidationHelper_ValidatesCommonInputs()
        {
            AssertEx.True(ValidationHelper.IsValidEmail("admin@kingdomprep.edu.gh"));
            AssertEx.False(ValidationHelper.IsValidEmail("admin@"));
            AssertEx.True(ValidationHelper.IsStrongPassword("Password123"));
            AssertEx.False(ValidationHelper.IsStrongPassword("password"));
            AssertEx.True(ValidationHelper.IsValidUsername("admin_1"));
            AssertEx.False(ValidationHelper.IsValidUsername("admin user"));
            AssertEx.True(ValidationHelper.IsValidAmount("250.50"));
            AssertEx.False(ValidationHelper.IsValidAmount("-10"));
        }

        private static async Task EmployeeRepository_FiltersByDepartmentAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new EmployeeRepository(database.ConnectionString);
                DataTable table = await repo.GetAsTableAsync(filterDepartment: "Teaching");

                AssertEx.Equal(1, table.Rows.Count);
                AssertEx.Equal("Kofi Boateng", table.Rows[0]["Full Name"].ToString());
                AssertEx.Equal("Teaching", table.Rows[0]["Department"].ToString());
            }
        }

        private static async Task EmployeeRepository_TreatsMaliciousFiltersAsLiteralsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new EmployeeRepository(database.ConnectionString);
                DataTable table = await repo.GetAsTableAsync(
                    filterId: "'; DROP TABLE Employee; --",
                    filterDepartment: "Teaching' OR '1'='1");

                AssertEx.Equal(0, table.Rows.Count);

                DataTable allRows = await repo.GetAsTableAsync();
                AssertEx.Equal(2, allRows.Rows.Count, "The Employee table should still exist and contain seeded rows.");
            }
        }

        private static async Task EmployeeRepository_RetrievesEmployeeByIdAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new EmployeeRepository(database.ConnectionString);
                DataTable table = await repo.GetAsTableAsync(filterDepartment: "Administration");
                AssertEx.Equal(1, table.Rows.Count);

                string id = table.Rows[0]["ID"].ToString();
                var employee = await repo.GetByIdAsync(id);

                AssertEx.NotNull(employee);
                AssertEx.Equal("Ama Mensah", employee.FullName);
                AssertEx.Equal("Administration", employee.Department);
            }
        }

        private static async Task StudentRepository_RoundTripsStudentRecordsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new StudentRepository(database.ConnectionString);
                var student = CreateValidStudent("BASIC 1");
                student.StudentID = "";
                student.Email = "ama.guardian@example.com";
                student.ProfilePhoto = new byte[] { 1, 2, 3 };

                AssertEx.True(await repo.AddAsync(student));
                AssertEx.False(string.IsNullOrWhiteSpace(student.StudentID), "AddAsync should return the generated SQL identity.");
                AssertEx.True(await repo.ExistsAsync(student.StudentID));

                var loaded = await repo.GetByIdAsync(student.StudentID);
                AssertEx.NotNull(loaded);
                AssertEx.Equal("Ama", loaded.FirstName);
                AssertEx.Equal("Mensah", loaded.LastName);
                AssertEx.Equal("BASIC 1", loaded.ClassID);

                var classRows = await repo.GetAsTableAsync(filterClass: "BASIC 1");
                AssertEx.Equal(1, classRows.Rows.Count);
                AssertEx.Equal(student.StudentID, classRows.Rows[0]["ID"].ToString());

                var page = await repo.GetPageAsTableAsync(page: 1, pageSize: 10, filterClass: "BASIC 1", search: "Ama");
                AssertEx.Equal(1, page.TotalCount);
                AssertEx.Equal(1, page.Items.Rows.Count);
                AssertEx.Equal(student.StudentID, page.Items.Rows[0]["ID"].ToString());

                loaded.FirstName = "Akua";
                loaded.ClassID = "BASIC 2";
                AssertEx.True(await repo.UpdateAsync(loaded));

                var updated = await repo.GetByIdAsync(student.StudentID);
                AssertEx.Equal("Akua", updated.FirstName);
                AssertEx.Equal("BASIC 2", updated.ClassID);
            }
        }

        private static async Task StudentRepository_ReturnsStudentIdentityWithInsertTriggersAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                using (var connection = new SqlConnection(database.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        CREATE TABLE StudentInsertAudit (
                            AuditID INT IDENTITY(5000,1) NOT NULL PRIMARY KEY,
                            StudentID INT NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                        )", connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    using (var command = new SqlCommand(@"
                        CREATE TRIGGER trg_Students_Audit
                        ON Students
                        AFTER INSERT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            INSERT INTO StudentInsertAudit (StudentID)
                            SELECT StudentID FROM inserted;
                        END", connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                var repo = new StudentRepository(database.ConnectionString);
                var student = CreateValidStudent("BASIC 1");
                student.StudentID = "";

                AssertEx.True(await repo.AddAsync(student));
                AssertEx.Equal("1", student.StudentID);
                AssertEx.True(await repo.ExistsAsync(student.StudentID));

                using (var connection = new SqlConnection(database.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT MAX(AuditID) FROM StudentInsertAudit", connection))
                    {
                        var auditId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        AssertEx.True(auditId >= 5000, "The trigger should insert into its own identity table.");
                        AssertEx.NotEqual(auditId.ToString(), student.StudentID);
                    }
                }
            }
        }

        private static async Task FeeRepository_RecordsFeesAndPaymentsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new FeeRepository(database.ConnectionString);

                AssertEx.True(await repo.AddInitialFeeRecordAsync("501", "BASIC 1", 1000m));
                AssertEx.Equal(1000m, await repo.GetDefaultBalanceAsync("501", "BASIC 1"));

                AssertEx.True(await repo.UpdateFeeRecordAsync("501", "BASIC 2", 1200m));
                AssertEx.Equal(1200m, await repo.GetDefaultBalanceAsync("501", "BASIC 2"));

                AssertEx.True(await repo.AddInitialPaymentRecordAsync("501", "BASIC 2", "Ama Mensah", 1200m));
                AssertEx.True(await repo.AddPaymentRecordAsync(
                    "501",
                    "BASIC 2",
                    "Ama Mensah",
                    amountPaid: 300m,
                    newBalance: 900m,
                    paymentMode: "Cash",
                    bursarName: "Accounts",
                    date: DateTime.Today));

                AssertEx.Equal(900m, await repo.GetLatestBalanceAsync("501"));

                var fees = await repo.GetFeesTableAsync();
                AssertEx.Equal(1, fees.Rows.Count);
                AssertEx.Equal(1200m, Convert.ToDecimal(fees.Rows[0]["AMOUNT"]));

                var history = await repo.GetStudentPaymentHistoryTableAsync("501");
                AssertEx.Equal(2, history.Rows.Count);

                var firstPage = await repo.GetPaymentHistoryPageAsync(page: 1, pageSize: 1, search: "Ama");
                AssertEx.Equal(2, firstPage.TotalCount);
                AssertEx.Equal(1, firstPage.Items.Rows.Count);
                AssertEx.Equal(1200m, Convert.ToDecimal(firstPage.Items.Rows[0]["PREVIOUS BALANCE"]));
                AssertEx.Equal(900m, Convert.ToDecimal(firstPage.Items.Rows[0]["BALANCE"]));
                AssertEx.Equal(300m, Convert.ToDecimal(firstPage.Items.Rows[0]["AMOUNT PAID"]));

                var secondPage = await repo.GetPaymentHistoryPageAsync(page: 2, pageSize: 1, search: "Ama");
                AssertEx.Equal(2, secondPage.TotalCount);
                AssertEx.Equal(1, secondPage.Items.Rows.Count);
                AssertEx.Equal(0m, Convert.ToDecimal(secondPage.Items.Rows[0]["PREVIOUS BALANCE"]));
                AssertEx.Equal(1200m, Convert.ToDecimal(secondPage.Items.Rows[0]["BALANCE"]));

                var outstanding = await repo.GetOutstandingBalancesTableAsync();
                AssertEx.Equal(1, outstanding.Rows.Count);
                AssertEx.Equal(900m, Convert.ToDecimal(outstanding.Rows[0]["Balance Owed"]));
            }
        }

        private static async Task ExamRepository_RoundTripsExamResultsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new ExamRepository(database.ConnectionString);
                var result = CreateExamResult(category1: 30m, category2: 10m, category3: 10m, examScore: 45m);
                result.Calculate();

                AssertEx.False(await repo.ResultExistsAsync(result.StudentId, result.Subject, result.Term, result.Year));
                AssertEx.True(await repo.AddResultAsync(result));
                AssertEx.True(await repo.ResultExistsAsync(result.StudentId, result.Subject, result.Term, result.Year));

                var studentResults = await repo.GetStudentResultsAsync(result.StudentId, result.Term, result.Year);
                AssertEx.Equal(1, studentResults.Rows.Count);
                AssertEx.Equal("Mathematics", studentResults.Rows[0]["subject"].ToString());
                AssertEx.Equal(86.67m, Convert.ToDecimal(studentResults.Rows[0]["gt"]));

                result.ExamScore = 35m;
                result.Calculate();
                AssertEx.True(await repo.UpdateResultAsync(result));

                var classResults = await repo.GetClassSubjectResultsAsync(result.ClassId, result.Subject, result.Term, result.Year);
                AssertEx.Equal(1, classResults.Rows.Count);
                AssertEx.Equal(76.67m, Convert.ToDecimal(classResults.Rows[0]["gt"]));
                AssertEx.Equal("2", classResults.Rows[0]["grade"].ToString());

                var allResults = await repo.GetAllResultsTableAsync();
                AssertEx.Equal(1, allResults.Rows.Count);
                AssertEx.Equal(result.StudentId, allResults.Rows[0]["StudentID"].ToString());
            }
        }

        private static async Task AttendanceRepository_SavesAndAnalyzesAttendanceAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var studentRepo = new StudentRepository(database.ConnectionString);
                var student = CreateValidStudent("BASIC 1");
                student.StudentID = "";
                AssertEx.True(await studentRepo.AddAsync(student));

                var repo = new AttendanceRepository(database.ConnectionString);
                var date = new DateTime(2026, 6, 28);

                AssertEx.True(await repo.SaveAttendanceBatchAsync(new[]
                {
                    new AttendanceRecord
                    {
                        ReferenceID = student.StudentID,
                        ReferenceType = "STUDENT",
                        FullName = student.FullName,
                        Date = date,
                        Status = "PRESENT",
                        Remarks = "Morning"
                    }
                }));

                var targets = await repo.GetTargetListAsync("STUDENT", "BASIC 1", date);
                AssertEx.Equal(1, targets.Rows.Count);
                AssertEx.Equal("PRESENT", targets.Rows[0]["Status"].ToString());

                AssertEx.True(await repo.SaveAttendanceBatchAsync(new[]
                {
                    new AttendanceRecord
                    {
                        ReferenceID = student.StudentID,
                        ReferenceType = "STUDENT",
                        FullName = student.FullName,
                        Date = date,
                        Status = "LATE",
                        Remarks = "Transport delay"
                    }
                }));

                targets = await repo.GetTargetListAsync("STUDENT", "BASIC 1", date);
                AssertEx.Equal("LATE", targets.Rows[0]["Status"].ToString());
                AssertEx.Equal("Transport delay", targets.Rows[0]["Remarks"].ToString());

                var analysis = await repo.GetMonthlyAnalysisAsync("STUDENT", 6, 2026);
                AssertEx.Equal(1, analysis.Rows.Count);
                AssertEx.Equal(1, Convert.ToInt32(analysis.Rows[0]["Late"]));

                var classes = await repo.GetActiveClassesAsync();
                AssertEx.True(new List<string>(classes).Contains("BASIC 1"));
            }
        }

        private static async Task SubjectRepository_ManagesClassSubjectsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new SubjectRepository(database.ConnectionString);
                await repo.EnsureTableAsync();

                var seeded = await repo.GetSubjectsForClassAsync("BASIC 1");
                AssertEx.True(seeded.Count > 0, "EnsureTableAsync should seed default subjects.");

                await repo.SetSubjectsForClassAsync("BASIC 1", new[] { "MATHEMATICS", "ENGLISH LANGUAGE" });
                var subjects = await repo.GetSubjectsForClassAsync("BASIC 1");
                AssertEx.Equal(2, subjects.Count);
                AssertEx.Equal("MATHEMATICS", subjects[0]);
                AssertEx.Equal("ENGLISH LANGUAGE", subjects[1]);

                var all = await repo.GetAllAsync();
                AssertEx.True(all.ContainsKey("BASIC 1"));
                AssertEx.Equal(2, all["BASIC 1"].Count);
            }
        }

        private static async Task ClassRepository_ManagesClassesAndAssignmentsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var repo = new ClassRepository(database.ConnectionString);
                await repo.EnsureTableExistsAsync();

                var classes = await repo.GetAllClassesTableAsync();
                AssertEx.True(classes.Rows.Count > 0, "EnsureTableExistsAsync should seed configured classes.");

                AssertEx.True(await repo.SaveClassAsync(new ClassConfig
                {
                    ClassName = "BASIC TEST",
                    TuitionFee = 1234m,
                    PromotionLevel = 99
                }));

                var saved = await repo.GetByClassNameAsync("BASIC TEST");
                AssertEx.NotNull(saved);
                AssertEx.Equal(1234m, saved.TuitionFee);

                AssertEx.True(await repo.SaveClassAsync(new ClassConfig
                {
                    ClassName = "BASIC TEST 2",
                    TuitionFee = 1500m,
                    PromotionLevel = 100
                }, originalClassName: "BASIC TEST"));

                var renamed = await repo.GetByClassNameAsync("BASIC TEST 2");
                AssertEx.NotNull(renamed);
                AssertEx.Equal(1500m, renamed.TuitionFee);

                AssertEx.True(await repo.AssignTeacherToClassAsync("BASIC TEST 2", 1));
                var teacherClasses = new List<string>(await repo.GetClassesForTeacherAsync(1));
                AssertEx.True(teacherClasses.Contains("BASIC TEST 2"));

                var assignments = new List<(string ClassName, int? CurrentTeacherID)>(await repo.GetAllClassAssignmentsAsync());
                AssertEx.True(assignments.Exists(a => a.ClassName == "BASIC TEST 2" && a.CurrentTeacherID == 1));

                var detailed = new List<ClassAssignment>(await repo.GetAllDetailedAssignmentsAsync());
                AssertEx.True(detailed.Exists(a => a.ClassName == "BASIC TEST 2" && a.ClassTeacherName == "Ama Mensah"));

                AssertEx.True(await repo.DeleteClassAsync("BASIC TEST 2"));
                AssertEx.Null(await repo.GetByClassNameAsync("BASIC TEST 2"));
            }
        }

        private static async Task DashboardRepository_ReturnsCoreMetricsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var studentRepo = new StudentRepository(database.ConnectionString);
                var student = CreateValidStudent("BASIC 1");
                student.StudentID = "";
                AssertEx.True(await studentRepo.AddAsync(student));

                var feeRepo = new FeeRepository(database.ConnectionString);
                AssertEx.True(await feeRepo.AddPaymentRecordAsync(
                    student.StudentID,
                    "BASIC 1",
                    student.FullName,
                    amountPaid: 250m,
                    newBalance: 750m,
                    paymentMode: "Cash",
                    bursarName: "Accounts",
                    date: new DateTime(2026, 6, 28)));

                var exam = CreateExamResult(category1: 30m, category2: 10m, category3: 10m, examScore: 45m);
                exam.StudentId = student.StudentID;
                exam.StudentName = student.FullName;
                exam.ClassId = "BASIC 1";
                exam.Calculate();
                AssertEx.True(await new ExamRepository(database.ConnectionString).AddResultAsync(exam));

                await ExecuteSqlAsync(database.ConnectionString,
                    "INSERT INTO emp_leave (employeeID, employeeName, startDate, endDate, [status]) VALUES ('1', 'Ama Mensah', '2026-06-01', '2026-06-03', 'PENDING')");
                await ExecuteSqlAsync(database.ConnectionString,
                    "INSERT INTO Expenses (Expenses_name, Purpose, Amount, Date_Time) VALUES ('Exercise Books', 'Stationery', '125.50', '2026-06-10')");
                await ExecuteSqlAsync(database.ConnectionString,
                    "INSERT INTO ClassAssignments (ClassName, ClassTeacherID) VALUES ('BASIC 1', 1)");

                var repo = new DashboardRepository(database.ConnectionString);
                var metrics = await repo.GetCoreMetricsAsync();
                AssertEx.Equal(1, metrics.StudentCount);
                AssertEx.Equal(2, metrics.EmployeeCount);
                AssertEx.Equal(1, metrics.PendingLeaveCount);
                AssertEx.Equal(250m, metrics.TotalFeesCollected);
                AssertEx.Equal(750m, metrics.TotalFeesBalance);
                AssertEx.Equal("BASIC 1", metrics.TopClass);
                AssertEx.Equal("Stationery", metrics.TopExpenseCategory);

                var recentPayments = await repo.GetRecentPaymentsAsync(5);
                AssertEx.Equal(1, recentPayments.Rows.Count);

                var feeTrend = await repo.GetMonthlyFeeCollectionTrendAsync(2026);
                AssertEx.Equal(1, feeTrend.Rows.Count);
                AssertEx.Equal(6, Convert.ToInt32(feeTrend.Rows[0]["Mo"]));

                var scoresBySubject = await repo.GetAverageScoreBySubjectAsync();
                AssertEx.Equal(1, scoresBySubject.Rows.Count);
                AssertEx.Equal("Mathematics", scoresBySubject.Rows[0]["subject"].ToString());
            }
        }

        private static async Task DashboardSummaryRepository_RefreshesMonthlyMetricsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                var studentRepo = new StudentRepository(database.ConnectionString);
                var student = CreateValidStudent("BASIC 1");
                student.StudentID = "";
                AssertEx.True(await studentRepo.AddAsync(student));

                var date = new DateTime(2026, 6, 28);
                AssertEx.True(await new FeeRepository(database.ConnectionString).AddPaymentRecordAsync(
                    student.StudentID,
                    "BASIC 1",
                    student.FullName,
                    amountPaid: 500m,
                    newBalance: 250m,
                    paymentMode: "Cash",
                    bursarName: "Accounts",
                    date: date));

                AssertEx.True(await new AttendanceRepository(database.ConnectionString).SaveAttendanceBatchAsync(new[]
                {
                    new AttendanceRecord
                    {
                        ReferenceID = student.StudentID,
                        ReferenceType = "STUDENT",
                        FullName = student.FullName,
                        Date = date,
                        Status = "PRESENT",
                        Remarks = ""
                    }
                }));

                AssertEx.True(await new ExpenseRepository(database.ConnectionString).AddAsync(new Expense
                {
                    Name = "Books",
                    Category = "Stationery",
                    Description = "",
                    Date = date,
                    Amount = 125m,
                    Payee = "Supplier",
                    Payer = "Accounts"
                }));

                var summaryRepo = new DashboardSummaryRepository(database.ConnectionString);
                await summaryRepo.RefreshMonthAsync(date);
                var summary = await summaryRepo.GetMonthlySummaryAsync(2026);

                AssertEx.Equal(1, summary.Rows.Count);
                AssertEx.Equal(6, Convert.ToInt32(summary.Rows[0]["Mo"]));
                AssertEx.Equal(500m, Convert.ToDecimal(summary.Rows[0]["FeeCollected"]));
                AssertEx.Equal(250m, Convert.ToDecimal(summary.Rows[0]["FeeBalance"]));
                AssertEx.Equal(125m, Convert.ToDecimal(summary.Rows[0]["ExpenseTotal"]));
                AssertEx.Equal(100m, Convert.ToDecimal(summary.Rows[0]["AttendanceRate"]));

                var dashboard = new DashboardRepository(database.ConnectionString);
                var feeTrend = await dashboard.GetMonthlyFeeCollectionTrendAsync(2026);
                AssertEx.Equal(500m, Convert.ToDecimal(feeTrend.Rows[0]["Total"]));

                var attendanceTrend = await dashboard.GetMonthlyAttendanceRateAsync(2026);
                AssertEx.Equal(100m, Convert.ToDecimal(attendanceTrend.Rows[0]["RatePct"]));

                var incomeExpense = await dashboard.GetMonthlyIncomeVsExpensesAsync(2026);
                AssertEx.Equal(500m, Convert.ToDecimal(incomeExpense.Rows[0]["Income"]));
                AssertEx.Equal(125m, Convert.ToDecimal(incomeExpense.Rows[0]["Expense"]));
            }
        }

        private static async Task NoticeRepository_SavesAndLoadsHistoryAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await ExecuteSqlAsync(database.ConnectionString, @"
                    INSERT INTO Students
                    (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies,
                     EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, Std_pic)
                    VALUES
                    ('Ama', 'Test', '2018-01-01', 'FEMALE', 'ama.parent@example.com', 'BASIC 1',
                     'Accra', 'Accra', '', '0241234567', 'Parent One', 'parent@example.com', 'Accra', '2026-01-01', 0x)");

                var repository = new NoticeRepository(database.ConnectionString);
                var service = new NoticeService(repository);
                var notice = new Notice
                {
                    Title = "PTA Meeting",
                    Message = "Meeting at 2pm",
                    Target = "All",
                    Channel = "Both",
                    SentBy = "Admin",
                    SentDate = new DateTime(2026, 7, 4, 9, 0, 0),
                    Status = "Sent"
                };

                var sent = await service.SendNoticeAsync(notice);
                AssertEx.True(sent.Success, sent.Message);
                AssertEx.True(notice.NoticeID > 0);
                AssertEx.Equal(3, notice.RecipientCount, "All notices should count students plus employees even before legacy tables receive SchoolId.");

                var history = await service.GetHistoryTableAsync();
                AssertEx.Equal(1, history.Rows.Count);
                AssertEx.True(history.Columns.Contains("Target"));
                AssertEx.True(history.Columns.Contains("TargetClass"));
                AssertEx.Equal("PTA Meeting", history.Rows[0]["Title"].ToString());
                AssertEx.Equal("All", history.Rows[0]["Target"].ToString());
                AssertEx.Equal("Both", history.Rows[0]["Channel"].ToString());
            }
        }

        private static void Timetable_DefaultPeriodsMatchCommonSchoolDay()
        {
            var periods = DefaultTimetablePeriods.Create();

            AssertEx.Equal(12, periods.Count);
            AssertEx.Equal("Silence Hour", periods[0].PeriodName);
            AssertEx.Equal(TimeSpan.Parse("07:15"), periods[0].StartTime);
            AssertEx.Equal(TimeSpan.Parse("07:45"), periods[0].EndTime);
            AssertEx.True(periods[0].IsBreak, "Silence Hour should be treated as fixed non-teaching time.");

            AssertEx.Equal("Period 1", periods[2].PeriodName);
            AssertEx.False(periods[2].IsBreak);
            AssertEx.Equal("First Break", periods[4].PeriodName);
            AssertEx.True(periods[4].IsBreak);
            AssertEx.Equal("Lunch Time", periods[7].PeriodName);
            AssertEx.True(periods[7].IsBreak);
            AssertEx.Equal("Closing", periods[11].PeriodName);
            AssertEx.True(periods[11].IsBreak);

            AssertEx.Equal(6, periods.Count(p => !p.IsBreak), "The default day should contain six teaching periods.");
        }

        private static void TimetableDepartments_MapJuniorHighClasses()
        {
            AssertEx.Equal(TimetableDepartments.JuniorHighSchool, TimetableDepartments.GetDepartmentForClass("BASIC 7"));
            AssertEx.Equal(TimetableDepartments.JuniorHighSchool, TimetableDepartments.GetDepartmentForClass("BASIC 8"));
            AssertEx.Equal(TimetableDepartments.JuniorHighSchool, TimetableDepartments.GetDepartmentForClass("BASIC 9"));

            var classes = TimetableDepartments.GetClasses(TimetableDepartments.JuniorHighSchool);
            AssertEx.True(classes.Contains("BASIC 7"), "Junior High School should include BASIC 7.");
            AssertEx.True(classes.Contains("BASIC 8"), "Junior High School should include BASIC 8.");
            AssertEx.True(classes.Contains("BASIC 9"), "Junior High School should include BASIC 9.");
        }

        private static async Task TimetableGeneration_ExplainsInsufficientSlotsAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await EnsureTimetableTablesAsync(database.ConnectionString);

                var repo = new TimetableRepository(database.ConnectionString);
                await repo.SetPeriodsAsync(new[]
                {
                    new TimePeriod
                    {
                        PeriodName = "Period 1",
                        StartTime = TimeSpan.Parse("08:00"),
                        EndTime = TimeSpan.Parse("09:00"),
                        IsBreak = false,
                        SortOrder = 1
                    }
                });

                var generator = new TimetableGeneratorService(repo);
                var allocations = new Dictionary<string, List<SubjectAllocation>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BASIC 1"] = new List<SubjectAllocation>
                    {
                        new SubjectAllocation { ClassID = "BASIC 1", SubjectName = "English Language", TeacherID = 1, TeacherName = "Kofi Boateng", PeriodsPerWeek = 3 },
                        new SubjectAllocation { ClassID = "BASIC 1", SubjectName = "Mathematics", TeacherID = 2, TeacherName = "Ama Mensah", PeriodsPerWeek = 3 }
                    }
                };

                var result = await generator.GenerateForDepartmentAsync("Lower Primary", allocations);

                AssertEx.False(result.Success);
                AssertEx.Contains("selected workload needs more lesson slots", result.Message);
                AssertEx.Contains("Required periods: 6", result.Message);
                AssertEx.Contains("Available periods: 5", result.Message);
            }
        }

        private static async Task TimetableGeneration_SavesDepartmentBatchesAsync()
        {
            using (var database = await CreateIntegrationDatabaseOrSkipAsync())
            {
                await EnsureTimetableTablesAsync(database.ConnectionString);

                var repo = new TimetableRepository(database.ConnectionString);
                await repo.SetPeriodsAsync(DefaultTimetablePeriods.Create());

                var generator = new TimetableGeneratorService(repo);
                var allocations = new Dictionary<string, List<SubjectAllocation>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BASIC 7"] = new List<SubjectAllocation>
                    {
                        new SubjectAllocation { ClassID = "BASIC 7", SubjectName = "English Language", TeacherID = 1, TeacherName = "Kofi Boateng", PeriodsPerWeek = 2 },
                        new SubjectAllocation { ClassID = "BASIC 7", SubjectName = "Mathematics", TeacherID = 2, TeacherName = "Ama Mensah", PeriodsPerWeek = 2 }
                    },
                    ["BASIC 8"] = new List<SubjectAllocation>
                    {
                        new SubjectAllocation { ClassID = "BASIC 8", SubjectName = "English Language", TeacherID = 1, TeacherName = "Kofi Boateng", PeriodsPerWeek = 2 },
                        new SubjectAllocation { ClassID = "BASIC 8", SubjectName = "Mathematics", TeacherID = 2, TeacherName = "Ama Mensah", PeriodsPerWeek = 2 }
                    },
                    ["BASIC 9"] = new List<SubjectAllocation>
                    {
                        new SubjectAllocation { ClassID = "BASIC 9", SubjectName = "English Language", TeacherID = 1, TeacherName = "Kofi Boateng", PeriodsPerWeek = 2 },
                        new SubjectAllocation { ClassID = "BASIC 9", SubjectName = "Mathematics", TeacherID = 2, TeacherName = "Ama Mensah", PeriodsPerWeek = 2 }
                    }
                };

                var result = await generator.GenerateForDepartmentAsync("Junior High School", allocations, enforceTeacherConflicts: true);

                AssertEx.True(result.Success, result.Message);
                AssertEx.Equal(3, result.Batches.Count);

                var teacherConflict = result.Batches.Values
                    .SelectMany(batch => batch)
                    .Where(entry => entry.TeacherID.HasValue)
                    .GroupBy(entry => new { entry.TeacherID, entry.DayOfWeek, entry.PeriodID })
                    .FirstOrDefault(group => group.Count() > 1);
                AssertEx.Null(teacherConflict, "Department generation must not double-book a teacher in the same period.");

                await repo.SaveTimetableBatchesAsync(result.Batches);

                var basic7 = await repo.GetTimetableAsync("BASIC 7");
                var basic8 = await repo.GetTimetableAsync("BASIC 8");
                var basic9 = await repo.GetTimetableAsync("BASIC 9");
                AssertEx.Equal(4, basic7.Count);
                AssertEx.Equal(4, basic8.Count);
                AssertEx.Equal(4, basic9.Count);
                AssertEx.True(basic7.All(entry => entry.ClassID == "BASIC 7"));
                AssertEx.True(basic8.All(entry => entry.ClassID == "BASIC 8"));
                AssertEx.True(basic9.All(entry => entry.ClassID == "BASIC 9"));
            }
        }

        private static async Task<LocalDbTestDatabase> CreateIntegrationDatabaseOrSkipAsync()
        {
            try
            {
                return await LocalDbTestDatabase.CreateAsync();
            }
            catch (Exception ex)
            {
                throw new SkipTestException(ex.Message);
            }
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Nyansapo ERP School Management Software.sln")) ||
                    File.Exists(Path.Combine(directory.FullName, "kingdom_Preparatory_School_Management_System.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root for source audit.");
        }

        private static bool IsUnder(string path, string root, string segment)
        {
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var target = Path.Combine(Path.GetFullPath(root), segment).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return fullPath.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(target + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(target + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string MakeRelativePath(string root, string path)
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(fullRoot.Length + 1);
            }

            return fullPath;
        }

        private static async Task EnsureTimetableTablesAsync(string connectionString)
        {
            await ExecuteSqlAsync(connectionString, @"
                IF OBJECT_ID(N'TimePeriods', N'U') IS NULL
                BEGIN
                    CREATE TABLE TimePeriods (
                        PeriodID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        PeriodName NVARCHAR(100) NOT NULL,
                        StartTime TIME NOT NULL,
                        EndTime TIME NOT NULL,
                        IsBreak BIT NOT NULL,
                        SortOrder INT NOT NULL
                    )
                END");

            await ExecuteSqlAsync(connectionString, @"
                IF OBJECT_ID(N'SubjectAllocations', N'U') IS NULL
                BEGIN
                    CREATE TABLE SubjectAllocations (
                        AllocationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        ClassID NVARCHAR(50) NOT NULL,
                        SubjectName NVARCHAR(100) NOT NULL,
                        TeacherID INT NULL,
                        PeriodsPerWeek INT NOT NULL
                    )
                END");

            await ExecuteSqlAsync(connectionString, @"
                IF OBJECT_ID(N'TimetableEntries', N'U') IS NULL
                BEGIN
                    CREATE TABLE TimetableEntries (
                        EntryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        ClassID NVARCHAR(50) NOT NULL,
                        PeriodID INT NOT NULL,
                        DayOfWeek INT NOT NULL,
                        SubjectName NVARCHAR(100) NOT NULL,
                        TeacherID INT NULL
                    )
                END");
        }

        private static async Task ExecuteSqlAsync(string connectionString, string sql)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task SeedAdditionalFeeStudentsAsync(string connectionString)
        {
            await ExecuteSqlAsync(connectionString, @"
INSERT INTO Students
    (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence,
     Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location,
     admission_date, Std_pic)
VALUES
    ('Ama', 'Boating', '2015-01-01', 'FEMALE', 'ama@example.com', 'BASIC 1', '', '', '', '0240000001', 'Parent One', 'p1@example.com', '', GETDATE(), 0x),
    ('Yaw', 'Mensah', '2014-01-01', 'MALE', 'yaw@example.com', 'BASIC 2', '', '', '', '0240000002', 'Parent Two', 'p2@example.com', '', GETDATE(), 0x),
    ('Kofi', 'Appiah', '2011-01-01', 'MALE', 'kofi@example.com', 'BASIC 7', '', '', '', '0240000003', 'Parent Three', 'p3@example.com', '', GETDATE(), 0x);");
        }

        private static async Task<int> ScalarIntAsync(SqlConnection connection, string sql, object p0)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@p0", p0 ?? DBNull.Value);
                var value = await command.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private static async Task<decimal> ScalarDecimalAsync(SqlConnection connection, string sql, object p0)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@p0", p0 ?? DBNull.Value);
                var value = await command.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
            }
        }

        private static async Task<bool> IndexExistsAsync(string connectionString, string tableName, string indexName)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"
                SELECT COUNT(*)
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(@tableName)
                  AND name = @indexName", connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@indexName", indexName);
                await connection.OpenAsync();
                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
        }

        private static StudentService CreateStudentService()
        {
            return new StudentService(
                new FakeStudentRepository(),
                new FakeFeeRepository(),
                (studentId, baseFee) => Task.FromResult(baseFee));
        }

        private static StudentService CreateStudentService(FakeStudentRepository studentRepository, FakeFeeRepository feeRepository)
        {
            return new StudentService(
                studentRepository,
                feeRepository,
                (studentId, baseFee) => Task.FromResult(baseFee));
        }

        private static Student CreateValidStudent(string classId)
        {
            return new Student
            {
                StudentID = "100",
                FirstName = "Ama",
                LastName = "Mensah",
                DateOfBirth = DateTime.Today.AddYears(-10),
                Gender = "FEMALE",
                ClassID = classId,
                Email = "",
                HomeTown = "Accra",
                Residence = "Accra",
                Allergies = "",
                GuardianName = "Guardian",
                GuardianEmail = "",
                GuardianLocation = "Accra",
                EmergencyContact = "",
                AdmissionDate = DateTime.Today
            };
        }

        private static DraftAdmission CreateValidDraftAdmission()
        {
            return new DraftAdmission
            {
                FirstName = "Ama",
                LastName = "Admission",
                DateOfBirth = new DateTime(2015, 1, 1),
                Gender = "FEMALE",
                ClassID = "BASIC 1",
                Email = "guardian@example.com",
                HomeTown = "Akim Oda",
                Residence = "Abenase",
                Allergies = "",
                GuardianName = "Parent Test",
                GuardianEmail = "parent@example.com",
                GuardianLocation = "Abenase",
                EmergencyContact = "0241234567",
                AdmissionDate = new DateTime(2026, 6, 1),
                ProfilePhoto = new byte[0],
                AdmissionFee = AdmissionFees.Amount,
                SchoolFeePaid = 500m,
                TermTotal = 1000m,
                PaymentMode = "Cash",
                SubmittedBy = "Admin",
                SubmittedDate = new DateTime(2026, 6, 1, 9, 0, 0)
            };
        }

        private static LeaveRequest CreateLeaveRequest(DateTime startDate, DateTime endDate)
        {
            return new LeaveRequest
            {
                EmployeeID = "EMP001",
                EmployeeName = "Kofi Boateng",
                Department = "Teaching",
                Position = "Teacher",
                LeaveOption = "With Pay",
                Reason = "Sick",
                StartDate = startDate,
                EndDate = endDate
            };
        }

        private static PaymentRecordRequest CreatePaymentRecordRequest(
            string studentId = "501",
            string classId = "BASIC 9",
            string studentName = "Ama Mensah",
            decimal currentBalance = 1000m,
            decimal amountPaid = 250m,
            string paymentMode = "Cash",
            string bursarName = "Mr Accounts")
        {
            return new PaymentRecordRequest
            {
                StudentId = studentId,
                ClassId = classId,
                StudentName = studentName,
                CurrentBalance = currentBalance,
                AmountPaid = amountPaid,
                PaymentMode = paymentMode,
                BursarName = bursarName,
                PaymentDate = new DateTime(2026, 6, 4)
            };
        }

        private static ExamResult CreateExamResult(
            decimal category1 = 30m,
            decimal category2 = 10m,
            decimal category3 = 10m,
            decimal examScore = 45m)
        {
            return new ExamResult
            {
                StudentId = "STU001",
                StudentName = "Ama Mensah",
                ClassId = "BASIC 9",
                Subject = "Mathematics",
                Term = "Term 3",
                Year = "2026",
                Category1 = category1,
                Category2 = category2,
                Category3 = category3,
                ExamScore = examScore
            };
        }

        private static void AssertExamGrade(decimal totalScore, string expectedGrade, string expectedRemark)
        {
            var examScore = Math.Min(totalScore, 50m);
            var requiredSbaScore = Math.Max(0m, totalScore - examScore);
            var rawCategoryTotal = Math.Round(requiredSbaScore / 50m * 60m, 2);
            var result = CreateExamResult(category1: rawCategoryTotal, category2: 0m, category3: 0m, examScore: examScore);

            result.Calculate();

            AssertEx.Equal(totalScore, result.TotalScore);
            AssertEx.Equal(expectedGrade, result.Grade);
            AssertEx.Equal(expectedRemark, result.Remark);
        }

        private static void SetCurrentUser(string username, AuthService.UserRole role, int? employmentId = null)
        {
            var session = new AuthService.UserSession
            {
                Username = username,
                Role = role,
                EmploymentID = employmentId
            };

            var property = typeof(AuthService).GetProperty(
                "CurrentUser",
                BindingFlags.Static | BindingFlags.Public);

            property.GetSetMethod(true).Invoke(null, new object[] { session });
        }

        private sealed class TestCase
        {
            public TestCase(string name, Action run, bool isIntegration = false)
                : this(name, () =>
                {
                    run();
                    return Task.FromResult(true);
                }, isIntegration)
            {
            }

            public TestCase(string name, Func<Task> run, bool isIntegration = false)
            {
                Name = name;
                Run = run;
                IsIntegration = isIntegration;
            }

            public string Name { get; }
            public Func<Task> Run { get; }
            public bool IsIntegration { get; }
        }

        private sealed class SkipTestException : Exception
        {
            public SkipTestException(string message) : base(message)
            {
            }
        }

        private sealed class FakeStudentRepository : IStudentRepository
        {
            public bool AddResult { get; set; }
            public bool UpdateResult { get; set; }
            public bool DeleteResult { get; set; }
            public bool ExistsResult { get; set; }
            public bool BatchUpdateResult { get; set; }
            public bool RollOutResult { get; set; }
            public string GeneratedStudentId { get; set; } = "1";
            public int AddCallCount { get; private set; }
            public int UpdateCallCount { get; private set; }
            public int BatchUpdateCallCount { get; private set; }
            public Student LastAddedStudent { get; private set; }
            public Student LastUpdatedStudent { get; private set; }
            public string LastBatchClassId { get; private set; }
            public List<string> LastBatchStudentIds { get; private set; } = new List<string>();
            public Dictionary<string, Student> StudentsById { get; } = new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);

            public Task<Student> GetByIdAsync(string studentId)
            {
                Student student;
                return Task.FromResult(StudentsById.TryGetValue(studentId ?? "", out student) ? student : null);
            }

            public Task<IEnumerable<Student>> GetAllAsync()
            {
                return Task.FromResult<IEnumerable<Student>>(new Student[0]);
            }

            public Task<IEnumerable<Student>> GetByClassAsync(string classId)
            {
                return Task.FromResult<IEnumerable<Student>>(new Student[0]);
            }

            public Task<bool> AddAsync(Student student)
            {
                AddCallCount++;
                LastAddedStudent = student;
                if (student != null && string.IsNullOrWhiteSpace(student.StudentID))
                    student.StudentID = GeneratedStudentId;
                return Task.FromResult(AddResult);
            }

            public Task<bool> UpdateAsync(Student student)
            {
                UpdateCallCount++;
                LastUpdatedStudent = student;
                return Task.FromResult(UpdateResult);
            }

            public Task<bool> DeleteAsync(string studentId) => Task.FromResult(DeleteResult);
            public Task<bool> ExistsAsync(string studentId) => Task.FromResult(ExistsResult);
            public Task<string> GenerateNextStudentIdAsync() => Task.FromResult("999");
            public Task<string> GetNextStudentIdAsync() => Task.FromResult("999");
            public Task<DataTable> GetAsTableAsync(string filterId = null, string filterClass = null) => Task.FromResult(new DataTable());
            public Task<(DataTable Items, int TotalCount)> GetPageAsTableAsync(int page, int pageSize, string filterId = null, string filterClass = null, string search = null) => Task.FromResult((new DataTable(), 0));
            public Task<bool> UpdateStudentClassBatchAsync(IEnumerable<string> studentIds, string newClassId)
            {
                BatchUpdateCallCount++;
                LastBatchClassId = newClassId;
                LastBatchStudentIds = new List<string>(studentIds ?? new string[0]);
                return Task.FromResult(BatchUpdateResult);
            }
            public Task<bool> RollOutAsync(string studentId) => Task.FromResult(RollOutResult);
            public Task<bool> RestoreAsync(string studentId) => Task.FromResult(true);
            public Task<DataTable> GetRolledOutAsTableAsync() => Task.FromResult(new DataTable());
            public Task<DataTable> GetGraduatedAsTableAsync() => Task.FromResult(new DataTable());
        }

        private sealed class FakeFeeRepository : IFeeRepository
        {
            public int AddInitialFeeRecordCallCount { get; private set; }
            public int AddInitialPaymentRecordCallCount { get; private set; }
            public int UpdateFeeRecordCallCount { get; private set; }
            public int UpdatePaymentRecordCallCount { get; private set; }
            public string LastInitialFeeStudentId { get; private set; }
            public string LastInitialFeeClassId { get; private set; }
            public decimal LastInitialFeeAmount { get; private set; }
            public decimal LastInitialPaymentBalance { get; private set; }
            public string LastUpdatedFeeStudentId { get; private set; }
            public string LastUpdatedFeeClassId { get; private set; }
            public decimal LastUpdatedFeeAmount { get; private set; }
            public string LastUpdatedPaymentStudentId { get; private set; }
            public string LastUpdatedPaymentClassId { get; private set; }
            public string LastUpdatedPaymentStudentName { get; private set; }
            public decimal LastUpdatedPaymentBalance { get; private set; }
            public bool AddPaymentRecordResult { get; set; }
            public int AddPaymentRecordCallCount { get; private set; }
            public string LastPaymentStudentId { get; private set; }
            public string LastPaymentClassId { get; private set; }
            public string LastPaymentStudentName { get; private set; }
            public decimal LastPaymentAmountPaid { get; private set; }
            public decimal LastPaymentNewBalance { get; private set; }
            public string LastPaymentMode { get; private set; }
            public string LastPaymentBursarName { get; private set; }
            public DateTime LastPaymentDate { get; private set; }

            public Task<bool> AddInitialFeeRecordAsync(string studentId, string classId, decimal amount)
            {
                AddInitialFeeRecordCallCount++;
                LastInitialFeeStudentId = studentId;
                LastInitialFeeClassId = classId;
                LastInitialFeeAmount = amount;
                return Task.FromResult(true);
            }

            public Task<bool> AddInitialPaymentRecordAsync(string studentId, string classId, string studentName, decimal balance)
            {
                AddInitialPaymentRecordCallCount++;
                LastInitialPaymentBalance = balance;
                return Task.FromResult(true);
            }

            public Task<bool> UpdateFeeRecordAsync(string studentId, string classId, decimal amount)
            {
                UpdateFeeRecordCallCount++;
                LastUpdatedFeeStudentId = studentId;
                LastUpdatedFeeClassId = classId;
                LastUpdatedFeeAmount = amount;
                return Task.FromResult(true);
            }

            public Task<bool> UpdatePaymentRecordAsync(string studentId, string classId, string studentName, decimal balance)
            {
                UpdatePaymentRecordCallCount++;
                LastUpdatedPaymentStudentId = studentId;
                LastUpdatedPaymentClassId = classId;
                LastUpdatedPaymentStudentName = studentName;
                LastUpdatedPaymentBalance = balance;
                return Task.FromResult(true);
            }

            public Task<decimal?> GetLatestBalanceAsync(string studentId) => Task.FromResult<decimal?>(null);
            public Task<decimal?> GetDefaultBalanceAsync(string studentId, string classId) => Task.FromResult<decimal?>(null);

            public Task<bool> AddPaymentRecordAsync(string studentId, string classId, string studentName, decimal amountPaid, decimal newBalance, string paymentMode, string bursarName, DateTime date)
            {
                AddPaymentRecordCallCount++;
                LastPaymentStudentId = studentId;
                LastPaymentClassId = classId;
                LastPaymentStudentName = studentName;
                LastPaymentAmountPaid = amountPaid;
                LastPaymentNewBalance = newBalance;
                LastPaymentMode = paymentMode;
                LastPaymentBursarName = bursarName;
                LastPaymentDate = date;
                return Task.FromResult(AddPaymentRecordResult);
            }

            public Task<DataTable> GetPaymentHistoryTableAsync() => Task.FromResult(new DataTable());
            public Task<(DataTable Items, int TotalCount)> GetPaymentHistoryPageAsync(int page, int pageSize, string search = null) => Task.FromResult((new DataTable(), 0));
            public Task<DataTable> GetStudentPaymentHistoryTableAsync(string studentId) => Task.FromResult(new DataTable());
            public Task<DataTable> GetStudentLedgerTableAsync(string studentId) => Task.FromResult(new DataTable());
            public Task<DataTable> GetOutstandingBalancesTableAsync() => Task.FromResult(new DataTable());
            public Task<DataTable> GetFeesTableAsync() => Task.FromResult(new DataTable());
        }

        private sealed class FakeLeaveRepository : ILeaveRepository
        {
            public bool AddResult { get; set; }
            public bool UpdateResult { get; set; }
            public bool HasApprovedOverlapResult { get; set; }
            public int ApprovedDaysInRangeResult { get; set; }
            public int AddLeaveRequestCallCount { get; private set; }
            public int HasApprovedOverlapCallCount { get; private set; }
            public int GetApprovedDaysInRangeCallCount { get; private set; }
            public int GetAllLeaveRequestsTableCallCount { get; private set; }
            public int GetLeaveRequestsByStatusCallCount { get; private set; }
            public LeaveRequest LastAddedLeaveRequest { get; private set; }
            public LeaveRequest LastUpdatedLeaveRequest { get; private set; }
            public string LastOverlapEmployeeId { get; private set; }
            public DateTime LastOverlapStartDate { get; private set; }
            public DateTime LastOverlapEndDate { get; private set; }
            public string LastApprovedDaysEmployeeId { get; private set; }
            public DateTime LastApprovedDaysTermStart { get; private set; }
            public DateTime LastApprovedDaysTermEnd { get; private set; }
            public string LastStatusFilter { get; private set; }

            public Task<bool> AddLeaveRequestAsync(LeaveRequest request)
            {
                AddLeaveRequestCallCount++;
                LastAddedLeaveRequest = request;
                return Task.FromResult(AddResult);
            }

            public Task<bool> UpdateLeaveRequestAsync(LeaveRequest request)
            {
                LastUpdatedLeaveRequest = request;
                return Task.FromResult(UpdateResult);
            }

            public Task<bool> DeleteLeaveRequestAsync(int leaveId)
            {
                return Task.FromResult(false);
            }

            public Task<DataTable> GetAllLeaveRequestsTableAsync()
            {
                GetAllLeaveRequestsTableCallCount++;
                return Task.FromResult(new DataTable());
            }

            public Task<DataTable> GetLeaveRequestsByStatusAsync(string status)
            {
                GetLeaveRequestsByStatusCallCount++;
                LastStatusFilter = status;
                return Task.FromResult(new DataTable());
            }

            public Task<IEnumerable<LeaveRequest>> GetEmployeeLeaveHistoryAsync(string employeeId)
            {
                return Task.FromResult<IEnumerable<LeaveRequest>>(new LeaveRequest[0]);
            }

            public Task<int> GetApprovedDaysInRangeAsync(string employeeId, DateTime termStart, DateTime termEnd)
            {
                GetApprovedDaysInRangeCallCount++;
                LastApprovedDaysEmployeeId = employeeId;
                LastApprovedDaysTermStart = termStart;
                LastApprovedDaysTermEnd = termEnd;
                return Task.FromResult(ApprovedDaysInRangeResult);
            }

            public Task<bool> HasApprovedOverlapAsync(string employeeId, DateTime startDate, DateTime endDate)
            {
                HasApprovedOverlapCallCount++;
                LastOverlapEmployeeId = employeeId;
                LastOverlapStartDate = startDate;
                LastOverlapEndDate = endDate;
                return Task.FromResult(HasApprovedOverlapResult);
            }

            public Task<DataTable> GetLeaveBalanceTableAsync(DateTime termStart, DateTime termEnd, int entitlement)
            {
                return Task.FromResult(new DataTable());
            }
        }

        private sealed class FakeExamRepository : IExamRepository
        {
            public bool ResultExists { get; set; }
            public bool AddResult { get; set; }
            public bool UpdateResult { get; set; }
            public bool ThrowOnResultExists { get; set; }
            public int ResultExistsCallCount { get; private set; }
            public int AddResultCallCount { get; private set; }
            public int UpdateResultCallCount { get; private set; }
            public ExamResult LastAddedResult { get; private set; }
            public ExamResult LastUpdatedResult { get; private set; }

            public Task<bool> ResultExistsAsync(string studentId, string subject, string term, string year)
            {
                ResultExistsCallCount++;
                if (ThrowOnResultExists)
                    throw new InvalidOperationException("repository unavailable");
                return Task.FromResult(ResultExists);
            }

            public Task<bool> AddResultAsync(ExamResult result)
            {
                AddResultCallCount++;
                LastAddedResult = result;
                return Task.FromResult(AddResult);
            }

            public Task<bool> UpdateResultAsync(ExamResult result)
            {
                UpdateResultCallCount++;
                LastUpdatedResult = result;
                return Task.FromResult(UpdateResult);
            }

            public Task<DataTable> GetAllResultsTableAsync()
            {
                return Task.FromResult(new DataTable());
            }

            public Task<DataTable> GetStudentResultsAsync(string studentId, string term, string year)
            {
                return Task.FromResult(new DataTable());
            }

            public Task<DataTable> GetClassSubjectResultsAsync(string classId, string subject, string term, string year)
            {
                return Task.FromResult(new DataTable());
            }
        }
    }
}

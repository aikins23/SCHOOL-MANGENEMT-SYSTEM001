using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace Kingdom.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            return MainAsync().GetAwaiter().GetResult();
        }

        private static async Task<int> MainAsync()
        {
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
                new TestCase("SyncSchema adds sync columns idempotently", SyncSchema_AddsColumnsIdempotentlyAsync),
                new TestCase("TenantSchema covers core school-owned tables", TenantSchema_CoversCoreTables),
                new TestCase("SchoolInformation creates a school identity", SchoolInformation_CreatesSchoolIdentity),
                new TestCase("SecretStorage protects and restores local secrets", SecretStorage_ProtectsAndRestoresSecrets),
                new TestCase("SecretStorage preserves legacy plaintext values", SecretStorage_PreservesLegacyPlaintextValues),
                new TestCase("FeeBalanceCalculator calculates remaining balances", FeeBalanceCalculator_CalculatesRemainingBalances),
                new TestCase("FeeBalanceCalculator rejects invalid values", FeeBalanceCalculator_RejectsInvalidValues),
                new TestCase("PaymentService records calculated payment balances", PaymentService_RecordsCalculatedPaymentBalancesAsync),
                new TestCase("PaymentService records overpayments as zero balance", PaymentService_RecordsOverpaymentsAsZeroBalanceAsync),
                new TestCase("PaymentService validates required payment fields", PaymentService_ValidatesRequiredPaymentFieldsAsync),
                new TestCase("PaymentService reports repository save failures", PaymentService_ReportsRepositorySaveFailuresAsync),
                new TestCase("StudentService maps tuition fees by class", StudentService_MapsTuitionFeesByClass),
                new TestCase("StudentService maps every configured class to a non-default fee", StudentService_MapsEveryConfiguredClass),
                new TestCase("StudentService creates opening fee records on add", StudentService_CreatesOpeningFeeRecordsOnAddAsync),
                new TestCase("StudentService rejects invalid students before persistence", StudentService_RejectsInvalidStudentsBeforePersistenceAsync),
                new TestCase("StudentService rejects duplicate student IDs before fee creation", StudentService_RejectsDuplicateStudentIdsBeforeFeeCreationAsync),
                new TestCase("StudentService updates fee records on class change", StudentService_UpdatesFeeRecordsOnClassChangeAsync),
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
                new TestCase("AuthService hashes and verifies passwords", AuthService_HashesAndVerifiesPasswords),
                new TestCase("AuthService rejects invalid login credentials", AuthService_RejectsInvalidLoginCredentials),
                new TestCase("AuthService validates registration input", AuthService_ValidatesRegistrationInput),
                new TestCase("AuthService blocks protected screens while logged out", AuthService_BlocksProtectedScreensWhileLoggedOut),
                new TestCase("AuthService denies blank and unknown screen keys", AuthService_DeniesBlankAndUnknownScreenKeys),
                new TestCase("AuthService enforces role-specific screen access", AuthService_EnforcesRoleSpecificScreenAccess),
                new TestCase("ValidationHelper validates common school form inputs", ValidationHelper_ValidatesCommonInputs),
                new TestCase("EmployeeRepository integration filters by department", EmployeeRepository_FiltersByDepartmentAsync),
                new TestCase("EmployeeRepository integration treats malicious filters as literals", EmployeeRepository_TreatsMaliciousFiltersAsLiteralsAsync),
                new TestCase("EmployeeRepository integration retrieves employee by ID", EmployeeRepository_RetrievesEmployeeByIdAsync)
            };

            int passed = 0;
            int skipped = 0;
            Console.WriteLine("Kingdom Preparatory test suite");
            Console.WriteLine(new string('-', 38));

            foreach (var test in tests)
            {
                try
                {
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
            var result = CreateExamResult(category1: 32m, category2: 8m, category3: 8m, examScore: 80m);

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
            var result = CreateExamResult(category1: 24m, category2: 6m, category3: 6m, examScore: 70m);

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
            var result = CreateExamResult(category1: 30m, category2: 9m, category3: 9m, examScore: 90m);

            var saveResult = await service.SaveResultsAsync(new[] { result });

            AssertEx.True(saveResult.Success, saveResult.Message);
            AssertEx.Equal(1, repository.ResultExistsCallCount);
            AssertEx.Equal(0, repository.AddResultCallCount);
            AssertEx.Equal(1, repository.UpdateResultCallCount);
            AssertEx.Equal(85m, repository.LastUpdatedResult.TotalScore);
            AssertEx.Equal("1", repository.LastUpdatedResult.Grade);
            AssertEx.Equal("Advance", repository.LastUpdatedResult.Remark);
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
            AssertEx.False(AuthService.CanAccess("frmTeacherDashboard"));

            SetCurrentUser("teacher", AuthService.UserRole.Teacher, 12);
            AssertEx.True(AuthService.CanAccess("frmTeacherDashboard"));
            AssertEx.True(AuthService.CanAccess("EXAMS"));
            AssertEx.False(AuthService.CanAccess("frmEmployee"));

            SetCurrentUser("accountant", AuthService.UserRole.Accountant);
            AssertEx.True(AuthService.CanAccess("frmFessPayment"));
            AssertEx.False(AuthService.CanAccess("frmEmployee"));

            SetCurrentUser("parent", AuthService.UserRole.Parent);
            AssertEx.False(AuthService.CanAccess("frmDashboard"));

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

        private static StudentService CreateStudentService()
        {
            return new StudentService(new FakeStudentRepository(), new FakeFeeRepository());
        }

        private static StudentService CreateStudentService(FakeStudentRepository studentRepository, FakeFeeRepository feeRepository)
        {
            return new StudentService(studentRepository, feeRepository);
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
            decimal examScore = 90m)
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
            var result = CreateExamResult(category1: 0m, category2: 0m, category3: 0m, examScore: totalScore * 2m);

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
            public TestCase(string name, Action run)
                : this(name, () =>
                {
                    run();
                    return Task.FromResult(true);
                })
            {
            }

            public TestCase(string name, Func<Task> run)
            {
                Name = name;
                Run = run;
            }

            public string Name { get; }
            public Func<Task> Run { get; }
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
            public Student LastAddedStudent { get; private set; }
            public Student LastUpdatedStudent { get; private set; }

            public Task<Student> GetByIdAsync(string studentId)
            {
                return Task.FromResult<Student>(null);
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
            public Task<string> GenerateNextStudentIdAsync() => Task.FromResult("1");
            public Task<DataTable> GetAsTableAsync(string filterId = null, string filterClass = null) => Task.FromResult(new DataTable());
            public Task<bool> UpdateStudentClassBatchAsync(IEnumerable<string> studentIds, string newClassId) => Task.FromResult(BatchUpdateResult);
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
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class AdditionalFeeRepository : IAdditionalFeeRepository
    {
        private readonly string _connectionString;

        public AdditionalFeeRepository(string connectionString)
        {
            _connectionString = SqlCommandExtensions.StripProvider(connectionString ?? throw new ArgumentNullException(nameof(connectionString)));
        }

        public async Task EnsureSchemaAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(SchemaSql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> CreateAsync(AdditionalFee fee)
        {
            if (fee == null) throw new ArgumentNullException(nameof(fee));
            await EnsureSchemaAsync();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var schoolId = CurrentSchoolValue();
                        const string sql = @"
DECLARE @Inserted TABLE (AdditionalFeeId INT);
INSERT INTO AdditionalFees
    (FeeName, Description, AcademicYear, TermName, DueDate, AssignmentMode,
     DefaultAmount, IsCompulsory, AllowsPartPayment, NotifyParentsBySms, Status, CreatedBy, CreatedDate, SchoolId)
OUTPUT INSERTED.AdditionalFeeId INTO @Inserted
VALUES
    (@FeeName, @Description, @AcademicYear, @TermName, @DueDate, @AssignmentMode,
     @DefaultAmount, @IsCompulsory, @AllowsPartPayment, @NotifyParentsBySms, @Status, @CreatedBy, SYSUTCDATETIME(), @SchoolId);
SELECT TOP 1 AdditionalFeeId FROM @Inserted;";

                        int id;
                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            AddFeeParameters(command, fee, schoolId);
                            id = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        foreach (var amount in NormalizeAmounts(fee))
                        {
                            using (var command = new SqlCommand(@"
INSERT INTO AdditionalFeeAmounts (AdditionalFeeId, ScopeType, ScopeKey, Amount)
VALUES (@AdditionalFeeId, @ScopeType, @ScopeKey, @Amount);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@AdditionalFeeId", id);
                                command.Parameters.AddWithValue("@ScopeType", amount.ScopeType ?? AdditionalFeeScopeTypes.All);
                                command.Parameters.AddWithValue("@ScopeKey", amount.ScopeKey ?? "");
                                command.Parameters.AddWithValue("@Amount", amount.Amount);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await InsertAuditAsync(connection, transaction, id, "Created", fee.CreatedBy, null);
                        transaction.Commit();
                        fee.AdditionalFeeId = id;
                        return id;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<AdditionalFee> GetByIdAsync(int additionalFeeId)
        {
            await EnsureSchemaAsync();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT TOP 1 * FROM AdditionalFees WHERE AdditionalFeeId = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", additionalFeeId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync()) return null;
                        var fee = MapFee(reader);
                        fee.Amounts = (await GetAmountsAsync(additionalFeeId)).ToList();
                        return fee;
                    }
                }
            }
        }

        public async Task<IReadOnlyList<AdditionalFee>> GetRecentAsync(int take = 50)
        {
            await EnsureSchemaAsync();
            var fees = new List<AdditionalFee>();
            int rowLimit = Math.Max(1, Math.Min(take, 200));

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT TOP (@Take) *
FROM AdditionalFees
WHERE ((@SchoolId IS NULL AND SchoolId IS NULL) OR SchoolId = @SchoolId)
ORDER BY CreatedDate DESC, AdditionalFeeId DESC;", connection))
                {
                    command.Parameters.AddWithValue("@Take", rowLimit);
                    command.Parameters.AddWithValue("@SchoolId", CurrentSchoolValue());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fees.Add(MapFee(reader));
                        }
                    }
                }
            }

            return fees;
        }

        public async Task<IReadOnlyList<AdditionalFeeAmount>> GetAmountsAsync(int additionalFeeId)
        {
            await EnsureSchemaAsync();
            var amounts = new List<AdditionalFeeAmount>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT AmountId, AdditionalFeeId, ScopeType, ScopeKey, Amount
FROM AdditionalFeeAmounts
WHERE AdditionalFeeId = @Id
ORDER BY AmountId", connection))
                {
                    command.Parameters.AddWithValue("@Id", additionalFeeId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            amounts.Add(new AdditionalFeeAmount
                            {
                                AmountId = Convert.ToInt32(reader["AmountId"]),
                                AdditionalFeeId = Convert.ToInt32(reader["AdditionalFeeId"]),
                                ScopeType = Convert.ToString(reader["ScopeType"]),
                                ScopeKey = Convert.ToString(reader["ScopeKey"]),
                                Amount = Convert.ToDecimal(reader["Amount"])
                            });
                        }
                    }
                }
            }

            return amounts;
        }

        public async Task<bool> HasDuplicateOpenFeeAsync(AdditionalFee fee)
        {
            if (fee == null) throw new ArgumentNullException(nameof(fee));
            await EnsureSchemaAsync();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
SELECT COUNT(*)
FROM AdditionalFees
WHERE UPPER(FeeName) = UPPER(@FeeName)
  AND UPPER(AcademicYear) = UPPER(@AcademicYear)
  AND UPPER(TermName) = UPPER(@TermName)
  AND Status NOT IN (@Rejected, @Cancelled, @Closed)
  AND ((@SchoolId IS NULL AND SchoolId IS NULL) OR SchoolId = @SchoolId);", connection))
                {
                    command.Parameters.AddWithValue("@FeeName", fee.FeeName ?? "");
                    command.Parameters.AddWithValue("@AcademicYear", fee.AcademicYear ?? "");
                    command.Parameters.AddWithValue("@TermName", fee.TermName ?? "");
                    command.Parameters.AddWithValue("@Rejected", AdditionalFeeStatuses.Rejected);
                    command.Parameters.AddWithValue("@Cancelled", AdditionalFeeStatuses.Cancelled);
                    command.Parameters.AddWithValue("@Closed", AdditionalFeeStatuses.Closed);
                    command.Parameters.AddWithValue("@SchoolId", CurrentSchoolValue());
                    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                }
            }
        }

        public async Task<AdditionalFeePreview> PreviewAsync(int additionalFeeId)
        {
            var fee = await GetByIdAsync(additionalFeeId);
            if (fee == null) throw new InvalidOperationException("Additional fee was not found.");

            var students = await LoadStudentsAsync();
            var preview = new AdditionalFeePreview();
            foreach (var student in BuildStudentCharges(fee, students))
            {
                preview.Students.Add(student);
                preview.ExpectedTotal += student.Amount;
            }

            preview.StudentCount = preview.Students.Count;
            return preview;
        }

        public async Task SubmitAsync(int additionalFeeId, string submittedBy)
        {
            await ChangeStatusAsync(additionalFeeId, AdditionalFeeStatuses.PendingApproval, submittedBy, "Submitted", null);
        }

        public async Task RejectAsync(int additionalFeeId, string rejectedBy, string reason)
        {
            await EnsureSchemaAsync();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = new SqlCommand(@"
UPDATE AdditionalFees
SET Status = @Status, RejectedBy = @UserName, RejectedAt = SYSUTCDATETIME(), RejectionReason = @Reason
WHERE AdditionalFeeId = @Id;", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Status", AdditionalFeeStatuses.Rejected);
                            command.Parameters.AddWithValue("@UserName", rejectedBy ?? "");
                            command.Parameters.AddWithValue("@Reason", reason ?? "");
                            command.Parameters.AddWithValue("@Id", additionalFeeId);
                            await command.ExecuteNonQueryAsync();
                        }

                        await InsertAuditAsync(connection, transaction, additionalFeeId, "Rejected", rejectedBy, reason);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task ApproveAndPostAsync(int additionalFeeId, string approvedBy)
        {
            await EnsureSchemaAsync();
            var fee = await GetByIdAsync(additionalFeeId);
            if (fee == null) throw new InvalidOperationException("Additional fee was not found.");
            if (!string.Equals(fee.Status, AdditionalFeeStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only fees pending approval can be approved.");

            var students = await LoadStudentsAsync();
            var charges = BuildStudentCharges(fee, students).ToList();
            if (charges.Count == 0) throw new InvalidOperationException("No students match this additional fee.");

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                bool feesHasSchool = await HasColumnAsync(connection, "fees", "SchoolId");
                bool paymentHasSchool = await HasColumnAsync(connection, "payment_record", "SchoolId");
                var schoolId = CurrentSchoolValue();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var charge in charges)
                        {
                            bool alreadyPosted;
                            using (var command = new SqlCommand(@"
SELECT COUNT(*)
FROM AdditionalFeeStudentCharges
WHERE AdditionalFeeId = @FeeId AND StudentID = @StudentId;", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FeeId", additionalFeeId);
                                command.Parameters.AddWithValue("@StudentId", charge.StudentId ?? "");
                                alreadyPosted = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                            }

                            if (alreadyPosted) continue;

                            await InsertStudentChargeAsync(connection, transaction, additionalFeeId, charge, schoolId);
                            await InsertFeeRowAsync(connection, transaction, fee, charge, feesHasSchool, schoolId);

                            decimal currentBalance = await GetLatestBalanceAsync(connection, transaction, charge.StudentId);
                            decimal newBalance = currentBalance + charge.Amount;
                            await InsertBalanceRowAsync(connection, transaction, charge, newBalance, paymentHasSchool, schoolId);
                        }

                        using (var command = new SqlCommand(@"
UPDATE AdditionalFees
SET Status = @Status, ApprovedBy = @UserName, ApprovedAt = SYSUTCDATETIME()
WHERE AdditionalFeeId = @Id;", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Status", AdditionalFeeStatuses.Active);
                            command.Parameters.AddWithValue("@UserName", approvedBy ?? "");
                            command.Parameters.AddWithValue("@Id", additionalFeeId);
                            await command.ExecuteNonQueryAsync();
                        }

                        await InsertAuditAsync(connection, transaction, additionalFeeId, "Approved and posted", approvedBy, null);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            if (fee.NotifyParentsBySms)
            {
                await QueueParentSmsAsync(fee, charges);
            }
        }

        private async Task ChangeStatusAsync(int additionalFeeId, string status, string userName, string action, string note)
        {
            await EnsureSchemaAsync();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = new SqlCommand(@"
UPDATE AdditionalFees
SET Status = @Status, SubmittedBy = CASE WHEN @Status = @Pending THEN @UserName ELSE SubmittedBy END,
    SubmittedAt = CASE WHEN @Status = @Pending THEN SYSUTCDATETIME() ELSE SubmittedAt END
WHERE AdditionalFeeId = @Id;", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Status", status);
                            command.Parameters.AddWithValue("@Pending", AdditionalFeeStatuses.PendingApproval);
                            command.Parameters.AddWithValue("@UserName", userName ?? "");
                            command.Parameters.AddWithValue("@Id", additionalFeeId);
                            await command.ExecuteNonQueryAsync();
                        }

                        await InsertAuditAsync(connection, transaction, additionalFeeId, action, userName, note);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void AddFeeParameters(SqlCommand command, AdditionalFee fee, object schoolId)
        {
            command.Parameters.AddWithValue("@FeeName", fee.FeeName ?? "");
            command.Parameters.AddWithValue("@Description", fee.Description ?? "");
            command.Parameters.AddWithValue("@AcademicYear", fee.AcademicYear ?? "");
            command.Parameters.AddWithValue("@TermName", fee.TermName ?? "");
            command.Parameters.AddWithValue("@DueDate", (object)fee.DueDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@AssignmentMode", fee.AssignmentMode ?? AdditionalFeeAssignmentModes.Flat);
            command.Parameters.AddWithValue("@DefaultAmount", fee.DefaultAmount);
            command.Parameters.AddWithValue("@IsCompulsory", fee.IsCompulsory);
            command.Parameters.AddWithValue("@AllowsPartPayment", fee.AllowsPartPayment);
            command.Parameters.AddWithValue("@NotifyParentsBySms", fee.NotifyParentsBySms);
            command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(fee.Status) ? AdditionalFeeStatuses.Draft : fee.Status);
            command.Parameters.AddWithValue("@CreatedBy", fee.CreatedBy ?? "");
            command.Parameters.AddWithValue("@SchoolId", schoolId);
        }

        private static IReadOnlyList<AdditionalFeeAmount> NormalizeAmounts(AdditionalFee fee)
        {
            if (fee.Amounts != null && fee.Amounts.Count > 0) return fee.Amounts;

            return new[]
            {
                new AdditionalFeeAmount
                {
                    ScopeType = AdditionalFeeScopeTypes.All,
                    ScopeKey = "",
                    Amount = fee.DefaultAmount
                }
            };
        }

        private async Task<List<AdditionalFeeStudentCharge>> LoadStudentsAsync()
        {
            var students = new List<AdditionalFeeStudentCharge>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                bool hasSchool = await HasColumnAsync(connection, "Students", "SchoolId");
                bool hasEmergencyConatct = await HasColumnAsync(connection, "Students", "EmergencyConatct");
                bool hasEmergencyContact = await HasColumnAsync(connection, "Students", "EmergencyContact");
                string phoneColumn = hasEmergencyConatct
                    ? "ISNULL(EmergencyConatct, '')"
                    : hasEmergencyContact ? "ISNULL(EmergencyContact, '')" : "''";
                var query = @"
SELECT
    CONVERT(NVARCHAR(50), StudentID) AS StudentID,
    LTRIM(RTRIM(ISNULL(FirstName, '') + ' ' + ISNULL(LastName, ''))) AS StudentName,
    ISNULL(ClassID, '') AS ClassID,
    " + phoneColumn + @" AS GuardianPhone
FROM Students
WHERE ISNULL(ClassID, '') <> ''";
                if (hasSchool && TenantContext.CurrentSchoolId != Guid.Empty)
                    query += " AND SchoolId = @SchoolId";
                query += " ORDER BY ClassID, FirstName, LastName, StudentID";

                using (var command = new SqlCommand(query, connection))
                {
                    if (hasSchool && TenantContext.CurrentSchoolId != Guid.Empty)
                        command.Parameters.AddWithValue("@SchoolId", TenantContext.CurrentSchoolId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            students.Add(new AdditionalFeeStudentCharge
                            {
                                StudentId = Convert.ToString(reader["StudentID"]),
                                StudentName = Convert.ToString(reader["StudentName"]),
                                ClassId = Convert.ToString(reader["ClassID"]),
                                GuardianPhone = Convert.ToString(reader["GuardianPhone"])
                            });
                        }
                    }
                }
            }

            return students;
        }

        private static IEnumerable<AdditionalFeeStudentCharge> BuildStudentCharges(AdditionalFee fee, IEnumerable<AdditionalFeeStudentCharge> students)
        {
            var amounts = NormalizeAmounts(fee);
            foreach (var student in students)
            {
                decimal? amount = ResolveAmount(fee, amounts, student.ClassId);
                if (!amount.HasValue || amount.Value <= 0) continue;

                yield return new AdditionalFeeStudentCharge
                {
                    AdditionalFeeId = fee.AdditionalFeeId,
                    StudentId = student.StudentId,
                    StudentName = student.StudentName,
                    ClassId = student.ClassId,
                    Amount = amount.Value,
                    Status = "Active",
                    PostedDate = DateTime.Today,
                    GuardianPhone = student.GuardianPhone
                };
            }
        }

        private async Task QueueParentSmsAsync(AdditionalFee fee, IReadOnlyList<AdditionalFeeStudentCharge> charges)
        {
            var outbox = new SmsOutboxRepository(_connectionString);
            string schoolName = string.IsNullOrWhiteSpace(SchoolProfile.DisplayName)
                ? "the school"
                : SchoolProfile.DisplayName.Trim();
            var messages = new List<Tuple<string, string, string>>();

            foreach (var charge in charges)
            {
                string phone = PhoneNumberGh.NormalizeGh(charge.GuardianPhone);
                if (string.IsNullOrWhiteSpace(phone)) continue;

                string dueText = fee.DueDate.HasValue
                    ? " by " + fee.DueDate.Value.ToString("dd MMM yyyy")
                    : "";
                string message = $"Dear Guardian, {fee.FeeName} of GHS {charge.Amount:N2} has been added to {charge.StudentName}'s account for {fee.TermName} {fee.AcademicYear}{dueText}. - {schoolName} Accounts";
                messages.Add(Tuple.Create(phone, SmsSenderIds.FeeReminder, message));
            }

            await outbox.EnqueueBatchAsync(messages);
        }

        private static decimal? ResolveAmount(AdditionalFee fee, IReadOnlyList<AdditionalFeeAmount> amounts, string classId)
        {
            if (string.Equals(fee.AssignmentMode, AdditionalFeeAssignmentModes.Flat, StringComparison.OrdinalIgnoreCase))
                return fee.DefaultAmount;

            if (string.Equals(fee.AssignmentMode, AdditionalFeeAssignmentModes.Class, StringComparison.OrdinalIgnoreCase))
            {
                var match = amounts.FirstOrDefault(a =>
                    string.Equals(a.ScopeType, AdditionalFeeScopeTypes.Class, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Normalize(a.ScopeKey), Normalize(classId), StringComparison.OrdinalIgnoreCase));
                return match == null ? (decimal?)null : match.Amount;
            }

            if (string.Equals(fee.AssignmentMode, AdditionalFeeAssignmentModes.Department, StringComparison.OrdinalIgnoreCase))
            {
                var department = TimetableDepartments.GetDepartmentForClass(classId);
                var match = amounts.FirstOrDefault(a =>
                    string.Equals(a.ScopeType, AdditionalFeeScopeTypes.Department, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.ScopeKey, department, StringComparison.OrdinalIgnoreCase));
                return match == null ? (decimal?)null : match.Amount;
            }

            return null;
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Trim().Replace(".", "").Replace("-", " ").ToUpperInvariant();
        }

        private static AdditionalFee MapFee(SqlDataReader reader)
        {
            return new AdditionalFee
            {
                AdditionalFeeId = Convert.ToInt32(reader["AdditionalFeeId"]),
                FeeName = Convert.ToString(reader["FeeName"]),
                Description = Convert.ToString(reader["Description"]),
                AcademicYear = Convert.ToString(reader["AcademicYear"]),
                TermName = Convert.ToString(reader["TermName"]),
                DueDate = reader["DueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DueDate"]),
                AssignmentMode = Convert.ToString(reader["AssignmentMode"]),
                DefaultAmount = Convert.ToDecimal(reader["DefaultAmount"]),
                IsCompulsory = Convert.ToBoolean(reader["IsCompulsory"]),
                AllowsPartPayment = Convert.ToBoolean(reader["AllowsPartPayment"]),
                NotifyParentsBySms = reader["NotifyParentsBySms"] != DBNull.Value && Convert.ToBoolean(reader["NotifyParentsBySms"]),
                Status = Convert.ToString(reader["Status"]),
                CreatedBy = Convert.ToString(reader["CreatedBy"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                SubmittedBy = Convert.ToString(reader["SubmittedBy"]),
                SubmittedAt = reader["SubmittedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["SubmittedAt"]),
                ApprovedBy = Convert.ToString(reader["ApprovedBy"]),
                ApprovedAt = reader["ApprovedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ApprovedAt"]),
                RejectedBy = Convert.ToString(reader["RejectedBy"]),
                RejectedAt = reader["RejectedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["RejectedAt"]),
                RejectionReason = Convert.ToString(reader["RejectionReason"]),
                SchoolId = reader["SchoolId"] == DBNull.Value ? (Guid?)null : (Guid)reader["SchoolId"]
            };
        }

        private static async Task<bool> HasColumnAsync(SqlConnection connection, string tableName, string columnName)
        {
            var safeTable = (tableName ?? "").Replace("'", "''");
            var safeColumn = (columnName ?? "").Replace("'", "''");
            using (var command = new SqlCommand($"SELECT COL_LENGTH('{safeTable}', '{safeColumn}')", connection))
            {
                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static object CurrentSchoolValue()
        {
            var schoolId = TenantContext.CurrentSchoolId;
            return schoolId == Guid.Empty ? (object)DBNull.Value : schoolId;
        }

        private static async Task InsertAuditAsync(SqlConnection connection, SqlTransaction transaction, int additionalFeeId, string action, string actor, string note)
        {
            using (var command = new SqlCommand(@"
INSERT INTO AdditionalFeeAuditLog (AdditionalFeeId, Action, Actor, Note, ActionDate)
VALUES (@AdditionalFeeId, @Action, @Actor, @Note, SYSUTCDATETIME());", connection, transaction))
            {
                command.Parameters.AddWithValue("@AdditionalFeeId", additionalFeeId);
                command.Parameters.AddWithValue("@Action", action ?? "");
                command.Parameters.AddWithValue("@Actor", actor ?? "");
                command.Parameters.AddWithValue("@Note", (object)note ?? DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertStudentChargeAsync(SqlConnection connection, SqlTransaction transaction, int additionalFeeId, AdditionalFeeStudentCharge charge, object schoolId)
        {
            using (var command = new SqlCommand(@"
INSERT INTO AdditionalFeeStudentCharges
    (AdditionalFeeId, StudentID, StudentName, ClassID, Amount, Status, PostedDate, SchoolId)
VALUES
    (@AdditionalFeeId, @StudentID, @StudentName, @ClassID, @Amount, @Status, SYSUTCDATETIME(), @SchoolId);", connection, transaction))
            {
                command.Parameters.AddWithValue("@AdditionalFeeId", additionalFeeId);
                command.Parameters.AddWithValue("@StudentID", charge.StudentId ?? "");
                command.Parameters.AddWithValue("@StudentName", charge.StudentName ?? "");
                command.Parameters.AddWithValue("@ClassID", charge.ClassId ?? "");
                command.Parameters.AddWithValue("@Amount", charge.Amount);
                command.Parameters.AddWithValue("@Status", "Active");
                command.Parameters.AddWithValue("@SchoolId", schoolId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertFeeRowAsync(SqlConnection connection, SqlTransaction transaction, AdditionalFee fee, AdditionalFeeStudentCharge charge, bool feesHasSchool, object schoolId)
        {
            var sql = feesHasSchool
                ? "INSERT INTO fees (StudentID, ClassID, FeeName, Amount, SchoolId) VALUES (@StudentID, @ClassID, @FeeName, @Amount, @SchoolId);"
                : "INSERT INTO fees (StudentID, ClassID, FeeName, Amount) VALUES (@StudentID, @ClassID, @FeeName, @Amount);";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@StudentID", charge.StudentId ?? "");
                command.Parameters.AddWithValue("@ClassID", charge.ClassId ?? "");
                command.Parameters.AddWithValue("@FeeName", fee.FeeName ?? "Additional Fee");
                command.Parameters.AddWithValue("@Amount", charge.Amount);
                if (feesHasSchool) command.Parameters.AddWithValue("@SchoolId", schoolId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task<decimal> GetLatestBalanceAsync(SqlConnection connection, SqlTransaction transaction, string studentId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1 ISNULL(Balance, 0)
FROM payment_record
WHERE StudentID = @StudentID
ORDER BY [Date] DESC, tm DESC, ID DESC;", connection, transaction))
            {
                command.Parameters.AddWithValue("@StudentID", studentId ?? "");
                var result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        private static async Task InsertBalanceRowAsync(SqlConnection connection, SqlTransaction transaction, AdditionalFeeStudentCharge charge, decimal newBalance, bool paymentHasSchool, object schoolId)
        {
            var sql = paymentHasSchool
                ? @"INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name, SchoolId)
                    VALUES (@StudentID, @ClassID, @Balance, @StudentName, 0, @Date, @Time, @Mode, @Bursar, @SchoolId);"
                : @"INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name)
                    VALUES (@StudentID, @ClassID, @Balance, @StudentName, 0, @Date, @Time, @Mode, @Bursar);";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@StudentID", charge.StudentId ?? "");
                command.Parameters.AddWithValue("@ClassID", charge.ClassId ?? "");
                command.Parameters.AddWithValue("@Balance", newBalance);
                command.Parameters.AddWithValue("@StudentName", charge.StudentName ?? "");
                command.Parameters.AddWithValue("@Date", DateTime.Today);
                command.Parameters.AddWithValue("@Time", DateTime.Now.ToString("HH:mm:ss"));
                command.Parameters.AddWithValue("@Mode", "Additional Fee Posted");
                command.Parameters.AddWithValue("@Bursar", "SYSTEM");
                if (paymentHasSchool) command.Parameters.AddWithValue("@SchoolId", schoolId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private const string SchemaSql = @"
IF OBJECT_ID(N'AdditionalFees', N'U') IS NULL
BEGIN
    CREATE TABLE AdditionalFees (
        AdditionalFeeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdditionalFees PRIMARY KEY,
        FeeName NVARCHAR(120) NOT NULL,
        Description NVARCHAR(500) NULL,
        AcademicYear NVARCHAR(20) NOT NULL,
        TermName NVARCHAR(50) NOT NULL,
        DueDate DATE NULL,
        AssignmentMode NVARCHAR(30) NOT NULL,
        DefaultAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_AdditionalFees_DefaultAmount DEFAULT 0,
        IsCompulsory BIT NOT NULL CONSTRAINT DF_AdditionalFees_IsCompulsory DEFAULT 1,
        AllowsPartPayment BIT NOT NULL CONSTRAINT DF_AdditionalFees_AllowsPartPayment DEFAULT 1,
        NotifyParentsBySms BIT NOT NULL CONSTRAINT DF_AdditionalFees_NotifyParentsBySms DEFAULT 0,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_AdditionalFees_Status DEFAULT 'Draft',
        CreatedBy NVARCHAR(100) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AdditionalFees_CreatedDate DEFAULT SYSUTCDATETIME(),
        SubmittedBy NVARCHAR(100) NULL,
        SubmittedAt DATETIME2 NULL,
        ApprovedBy NVARCHAR(100) NULL,
        ApprovedAt DATETIME2 NULL,
        RejectedBy NVARCHAR(100) NULL,
        RejectedAt DATETIME2 NULL,
        RejectionReason NVARCHAR(500) NULL,
        SchoolId UNIQUEIDENTIFIER NULL
    );
END

IF OBJECT_ID(N'AdditionalFees', N'U') IS NOT NULL AND COL_LENGTH('AdditionalFees','NotifyParentsBySms') IS NULL
    ALTER TABLE AdditionalFees ADD NotifyParentsBySms BIT NOT NULL CONSTRAINT DF_AdditionalFees_NotifyParentsBySms DEFAULT 0;

IF OBJECT_ID(N'AdditionalFeeAmounts', N'U') IS NULL
BEGIN
    CREATE TABLE AdditionalFeeAmounts (
        AmountId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdditionalFeeAmounts PRIMARY KEY,
        AdditionalFeeId INT NOT NULL,
        ScopeType NVARCHAR(30) NOT NULL,
        ScopeKey NVARCHAR(100) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_AdditionalFeeAmounts_Fee FOREIGN KEY (AdditionalFeeId)
            REFERENCES AdditionalFees(AdditionalFeeId)
    );
END

IF OBJECT_ID(N'AdditionalFeeStudentCharges', N'U') IS NULL
BEGIN
    CREATE TABLE AdditionalFeeStudentCharges (
        ChargeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdditionalFeeStudentCharges PRIMARY KEY,
        AdditionalFeeId INT NOT NULL,
        StudentID NVARCHAR(50) NOT NULL,
        StudentName NVARCHAR(200) NULL,
        ClassID NVARCHAR(50) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_AdditionalFeeStudentCharges_Status DEFAULT 'Active',
        PostedDate DATETIME2 NOT NULL CONSTRAINT DF_AdditionalFeeStudentCharges_PostedDate DEFAULT SYSUTCDATETIME(),
        SchoolId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_AdditionalFeeStudentCharges_Fee FOREIGN KEY (AdditionalFeeId)
            REFERENCES AdditionalFees(AdditionalFeeId)
    );
END

IF OBJECT_ID(N'AdditionalFeeAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE AdditionalFeeAuditLog (
        AuditId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdditionalFeeAuditLog PRIMARY KEY,
        AdditionalFeeId INT NOT NULL,
        Action NVARCHAR(80) NOT NULL,
        Actor NVARCHAR(100) NULL,
        Note NVARCHAR(500) NULL,
        ActionDate DATETIME2 NOT NULL CONSTRAINT DF_AdditionalFeeAuditLog_ActionDate DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AdditionalFees_Status' AND object_id = OBJECT_ID(N'AdditionalFees'))
    CREATE INDEX IX_AdditionalFees_Status ON AdditionalFees(SchoolId, Status, AcademicYear, TermName);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AdditionalFeeStudentCharges_Fee_Student' AND object_id = OBJECT_ID(N'AdditionalFeeStudentCharges'))
    CREATE UNIQUE INDEX UX_AdditionalFeeStudentCharges_Fee_Student ON AdditionalFeeStudentCharges(AdditionalFeeId, StudentID);
";
    }
}

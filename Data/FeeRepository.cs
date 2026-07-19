using KingdomPrep.Shared.Models;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class FeeRepository : IFeeRepository
    {
        private readonly string _connectionString;
        private const string FeesTable = "fees";
        private const string PaymentRecordTable = "payment_record";
        private const string StudentsTable = "Students";

        public FeeRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<bool> AddInitialFeeRecordAsync(string studentId, string classId, decimal amount)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    var query = tenant
                        ? "INSERT INTO fees (StudentID, ClassID, FeeName, Amount, SchoolId) VALUES (?, ?, ?, ?, ?)"
                        : "INSERT INTO fees (StudentID, ClassID, FeeName, Amount) VALUES (?, ?, ?, ?)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        command.AddPositionalParameter(classId);
                        command.AddPositionalParameter("Tuition Fee");
                        command.AddPositionalParameter(amount);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0)
                        {
                            var feeId = await GetLastIdentityAsync(connection);
                            await TryRecordSyncUpsertAsync(FeesTable, "FeeID", feeId, "Insert");
                        }
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error adding initial fee record for student {studentId}", ex);
                throw new DataException("Error adding initial fee record", ex);
            }
        }

        public async Task<bool> AddInitialPaymentRecordAsync(string studentId, string classId, string studentName, decimal balance)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    // payment_record has no FeeName column — omit it from INSERT.
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = tenant
                        ? "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], SchoolId) VALUES (?, ?, ?, ?, ?, ?, ?)"
                        : "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date]) VALUES (?, ?, ?, ?, ?, ?)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        command.AddPositionalParameter(classId);
                        command.AddPositionalParameter(balance);
                        command.AddPositionalParameter(studentName);
                        command.AddPositionalParameter(0m);
                        command.AddPositionalParameter(DateTime.Today);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0)
                        {
                            var paymentId = await GetLastIdentityAsync(connection);
                            await TryRecordSyncUpsertAsync(PaymentRecordTable, "ID", paymentId, "Insert");
                        }
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error adding initial payment record for student {studentId}", ex);
                throw new DataException("Error adding initial payment record", ex);
            }
        }

        public async Task<bool> UpdateFeeRecordAsync(string studentId, string classId, decimal amount)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = "UPDATE fees SET ClassID = ?, FeeName = ?, Amount = ? WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(classId);
                        command.AddPositionalParameter("Tuition Fee");
                        command.AddPositionalParameter(amount);
                        command.AddPositionalParameter(studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0) await TryRecordSyncUpsertAsync(FeesTable, "StudentID", studentId, "Update");
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error updating fee record for student {studentId}", ex);
                throw new DataException("Error updating fee record", ex);
            }
        }

        public async Task<bool> UpdatePaymentRecordAsync(string studentId, string classId, string studentName, decimal balance)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    // payment_record has no FeeName column — omit it from UPDATE.
                    var query = "UPDATE payment_record SET classID = ?, Balance = ?, student_name = ? WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(classId);
                        command.AddPositionalParameter(balance);
                        command.AddPositionalParameter(studentName);
                        command.AddPositionalParameter(studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0) await TryRecordSyncUpsertAsync(PaymentRecordTable, "StudentID", studentId, "Update");
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error updating payment record for student {studentId}", ex);
                throw new DataException("Error updating payment record", ex);
            }
        }

        public async Task<decimal?> GetLatestBalanceAsync(string studentId)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    // [Date] is a date (no time) and [tm] is time(0) (second resolution),
                    // so two rows recorded in the same second — e.g. the two admission rows
                    // (admission fee carrying the full term total, then the school-fee
                    // payment carrying the smaller remaining) — tie on (Date, tm) with no
                    // tiebreaker, and the engine could return the wrong Balance. Break the
                    // tie with [Balance] ASC: among same-second rows the most-paid (latest)
                    // state always has the lowest balance, so this returns the true latest.
                    var query = "SELECT TOP 1 [Balance] FROM [payment_record] WHERE [StudentID] = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    query += " ORDER BY [Date] DESC, [tm] DESC, [Balance] ASC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving latest balance for student {studentId}", ex);
                throw new DataException("Error retrieving latest balance", ex);
            }
            return null;
        }

        public async Task<decimal?> GetDefaultBalanceAsync(string studentId, string classId)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = "SELECT TOP 1 [Amount] FROM [fees] WHERE [StudentID] = ? AND [ClassID] = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        command.AddPositionalParameter(classId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving default balance for student {studentId}", ex);
                throw new DataException("Error retrieving default balance", ex);
            }
            return null;
        }

        public async Task<bool> AddPaymentRecordAsync(string studentId, string classId, string studentName, decimal amountPaid, decimal newBalance, string paymentMode, string bursarName, DateTime date)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    // payment_record has no FeeName column — omit it from INSERT.
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = tenant
                        ? "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name, SchoolId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"
                        : "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        command.AddPositionalParameter(classId);
                        command.AddPositionalParameter(newBalance);
                        command.AddPositionalParameter(studentName);
                        command.AddPositionalParameter(amountPaid);
                        command.AddPositionalParameter(date);
                        command.AddPositionalParameter(DateTime.Now.ToString("HH:mm:ss"));
                        command.AddPositionalParameter(paymentMode);
                        command.AddPositionalParameter(bursarName);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            var paymentId = await GetLastIdentityAsync(connection);
                            await TryRecordSyncUpsertAsync(PaymentRecordTable, "ID", paymentId, "Insert");
                            DashboardSummaryRepository.RefreshMonthBestEffort(_connectionString, date);

                            var guardianEmail = await GetStudentGuardianEmailAsync(studentId);
                            if (!string.IsNullOrWhiteSpace(guardianEmail))
                            {
                                _ = NotificationService.SendPaymentReceivedAsync(
                                    studentName, guardianEmail, amountPaid, newBalance, date, classId);
                            }

                            var guardianPhone = await GetStudentGuardianPhoneAsync(studentId);
                            if (!string.IsNullOrWhiteSpace(guardianPhone))
                            {
                                _ = SmsService.SendPaymentReceivedAsync(
                                    guardianPhone, studentName, amountPaid, newBalance);
                            }
                        }

                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error adding payment record for student {studentId}", ex);
                throw new DataException("Error adding payment record", ex);
            }
        }

        private async Task<string> GetStudentGuardianEmailAsync(string studentId)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var emailColumn = await ColumnExistsAsync(connection, StudentsTable, "GuidianceEmail")
                        ? "GuidianceEmail"
                        : await ColumnExistsAsync(connection, StudentsTable, "GuidanceEmail")
                            ? "GuidanceEmail"
                            : null;
                    if (string.IsNullOrWhiteSpace(emailColumn)) return "";

                    var query = $"SELECT {emailColumn} FROM Students WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, StudentsTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning($"Payment email lookup skipped for student {studentId}: {ex.Message}");
                return "";
            }
        }

        private async Task<string> GetStudentGuardianPhoneAsync(string studentId)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var phoneColumn = await ColumnExistsAsync(connection, StudentsTable, "EmergencyConatct")
                        ? "EmergencyConatct"
                        : await ColumnExistsAsync(connection, StudentsTable, "EmergencyContact")
                            ? "EmergencyContact"
                            : null;
                    if (string.IsNullOrWhiteSpace(phoneColumn)) return "";

                    var query = $"SELECT {phoneColumn} FROM Students WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, StudentsTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning($"Payment SMS lookup skipped for student {studentId}: {ex.Message}");
                return "";
            }
        }

        public async Task<DataTable> GetPaymentHistoryTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = @"
                        SELECT
                            [StudentID] AS [STUDENT ID],
                            [ClassID] AS [CLASS ID],
                            [student_name] AS [STUDENT NAME],
                            [Amount_paid] AS [AMOUNT PAID],
                            [Balance] AS [BALANCE],
                            [Date] AS [PAYMENT DATE],
                            [tm] AS [PAYMENT TIME],
                            [payment_mode] AS [PAYMENT MODE],
                            [Bursor_name] AS [BURSAR NAME]
                        FROM [payment_record]
                        WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    query += " ORDER BY [Date] DESC, [tm] DESC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving payment history", ex);
                throw new DataException("Error retrieving payment history", ex);
            }
            return table;
        }

        public async Task<(DataTable Items, int TotalCount)> GetPaymentHistoryPageAsync(int page, int pageSize, string search = null)
        {
            var table = new DataTable();
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 500));

            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);

                    string where = " WHERE 1=1";
                    if (tenant) where += TenantContext.FilterClauseSql();
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        where += @" AND (
                            CONVERT(NVARCHAR(50), [StudentID]) LIKE @Search
                            OR [student_name] LIKE @Search
                            OR [classID] LIKE @Search
                            OR [Bursor_name] LIKE @Search
                            OR [payment_mode] LIKE @Search)";
                    }

                    int total;
                    using (var count = new SqlCommand($"SELECT COUNT(*) FROM [payment_record]{where}", connection))
                    {
                        AddPaymentHistoryPageParameters(count, tenant, search, includePaging: false, page: page, pageSize: pageSize);
                        total = Convert.ToInt32(await count.ExecuteScalarAsync());
                    }

                    string tenantWhere = tenant ? " WHERE 1=1" + TenantContext.FilterClauseSql() : " WHERE 1=1";
                    string historySearch = "";
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        historySearch = @" WHERE (
                            CONVERT(NVARCHAR(50), [StudentID]) LIKE @Search
                            OR [student_name] LIKE @Search
                            OR [classID] LIKE @Search
                            OR [Bursor_name] LIKE @Search
                            OR [payment_mode] LIKE @Search)";
                    }

                    var query = $@"
                        WITH History AS
                        (
                            SELECT
                                [ID],
                                [StudentID],
                                [ClassID],
                                [student_name],
                                [Amount_paid],
                                [Balance],
                                [Date],
                                [tm],
                                [payment_mode],
                                [Bursor_name],
                                LAG(ISNULL([Balance], 0), 1, 0) OVER (
                                    PARTITION BY [StudentID]
                                    ORDER BY [Date], [tm], [ID]) AS [PreviousBalance]
                            FROM [payment_record]
                            {tenantWhere}
                        )
                        SELECT
                            [ID] AS [TRANSACTION ID],
                            [StudentID] AS [STUDENT ID],
                            [ClassID] AS [CLASS ID],
                            [student_name] AS [STUDENT NAME],
                            [Amount_paid] AS [AMOUNT PAID],
                            [Balance] AS [BALANCE],
                            [PreviousBalance] AS [PREVIOUS BALANCE],
                            [Date] AS [PAYMENT DATE],
                            [tm] AS [PAYMENT TIME],
                            [payment_mode] AS [PAYMENT MODE],
                            [Bursor_name] AS [BURSAR NAME]
                        FROM History
                        {historySearch}
                        ORDER BY [Date] DESC, [tm] DESC, [ID] DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddPaymentHistoryPageParameters(command, tenant, search, includePaging: true, page: page, pageSize: pageSize);
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }

                    return (table, total);
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving paged payment history", ex);
                throw new DataException("Error retrieving paged payment history", ex);
            }
        }

        private static void AddPaymentHistoryPageParameters(SqlCommand command, bool tenant, string search, bool includePaging, int page, int pageSize)
        {
            if (tenant)
            {
                TenantContext.AddSchoolParameter(command);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                command.Parameters.AddWithValue("@Search", "%" + search.Trim() + "%");
            }

            if (includePaging)
            {
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);
            }
        }

        public async Task<DataTable> GetOutstandingBalancesTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    // One row per student = their latest payment. [Date]/[tm] alone can tie
                    // (date has no time, tm is second-resolution), so rank by
                    // [Date] DESC, [tm] DESC, [Balance] ASC — among same-second rows the
                    // most-paid (latest) state has the lowest balance. ROW_NUMBER picks
                    // exactly one row per student, so a student can't duplicate or vanish.
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = @"
                        SELECT [ID], [Student Name], [Class], [Balance Owed], [Last Payment]
                        FROM (
                            SELECT
                                p.StudentID    AS [ID],
                                p.student_name AS [Student Name],
                                p.classID      AS [Class],
                                p.Balance      AS [Balance Owed],
                                p.[Date]       AS [Last Payment],
                                ROW_NUMBER() OVER (
                                    PARTITION BY p.StudentID
                                    ORDER BY p.[Date] DESC, p.tm DESC, p.Balance ASC) AS rn
                            FROM payment_record p
                            WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql("p");
                    }

                    query += @"
                        ) latest
                        WHERE latest.rn = 1 AND latest.[Balance Owed] > 0
                        ORDER BY latest.[Balance Owed] DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving outstanding balances", ex);
                throw new DataException("Error retrieving outstanding balances", ex);
            }
            return table;
        }

        public async Task<DataTable> GetFeesTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    var query = "SELECT FeeID AS [FEE ID], StudentID AS [STUDENT ID], ClassID AS [CLASS ID], FeeName AS [FEE NAME], Amount AS [AMOUNT] FROM fees WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    query += " ORDER BY FeeID DESC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving fees table", ex);
                throw new DataException("Error retrieving fees table", ex);
            }
            return table;
        }

        public async Task<DataTable> GetStudentLedgerTableAsync(string studentId)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "StudentFeeLedger");
                    var query = @"
SELECT
    l.TermID AS [TERM ID],
    ISNULL(t.TermName, '') AS [TERM],
    ISNULL(l.PreviousBalance, 0) AS [PREVIOUS DEBT],
    ISNULL(l.CurrentTermCharge, l.TotalExpectedAmount) AS [CURRENT FEE],
    ISNULL(l.TotalPaidAmount, 0) AS [PAID],
    ISNULL(l.TotalExpectedAmount - l.TotalPaidAmount, 0) AS [BALANCE]
FROM StudentFeeLedger l
LEFT JOIN AcademicTerms t ON t.TermID = l.TermID
WHERE l.StudentID = ?";
                    if (tenant) query += TenantContext.FilterClauseSql("l");
                    query += " ORDER BY l.TermID DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        using (var adapter = new SqlDataAdapter(command)) await Task.Run(() => adapter.Fill(table));
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving student ledger", ex);
                throw new DataException("Error retrieving student ledger", ex);
            }
            return table;
        }

        public async Task<DataTable> GetStudentPaymentHistoryTableAsync(string studentId)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = @"
SELECT
    ID AS [PAYMENT ID],
    StudentID AS [STUDENT ID],
    student_name AS [STUDENT NAME],
    classID AS [CLASS ID],
    Amount_paid AS [AMOUNT PAID],
    Balance AS [BALANCE],
    payment_mode AS [PAYMENT MODE],
    Bursor_name AS [BURSAR NAME],
    [Date] AS [DATE]
FROM payment_record
WHERE StudentID = ?";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    query += " ORDER BY [Date] DESC, tm DESC, ID DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(studentId);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        using (var adapter = new SqlDataAdapter(command)) await Task.Run(() => adapter.Fill(table));
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving student payment history", ex);
                throw new DataException("Error retrieving student payment history", ex);
            }
            return table;
        }

        private static async Task<object> GetLastIdentityAsync(SqlConnection connection)
        {
            using (var cmd = new SqlCommand("SELECT @@IDENTITY", connection))
            {
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : value;
            }
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName)
        {
            var safeTable = (tableName ?? "").Replace("'", "''");
            var safeColumn = (columnName ?? "").Replace("'", "''");
            using (var cmd = new SqlCommand($"SELECT COL_LENGTH('{safeTable}', '{safeColumn}')", connection))
            {
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
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
                Services.LoggerHelper.LogWarning("Fee sync capture skipped: " + ex.Message);
            }
        }
    }
}

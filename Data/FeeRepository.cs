using System;
using System.Data;
using System.Data.OleDb;
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    var query = tenant
                        ? "INSERT INTO fees (StudentID, ClassID, FeeName, Amount, SchoolId) VALUES (?, ?, ?, ?, ?)"
                        : "INSERT INTO fees (StudentID, ClassID, FeeName, Amount) VALUES (?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "Tuition Fee");
                        command.Parameters.AddWithValue("?", amount);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // payment_record has no FeeName column — omit it from INSERT.
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = tenant
                        ? "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], SchoolId) VALUES (?, ?, ?, ?, ?, ?, ?)"
                        : "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date]) VALUES (?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", balance);
                        command.Parameters.AddWithValue("?", studentName);
                        command.Parameters.AddWithValue("?", 0m);
                        command.Parameters.AddWithValue("?", DateTime.Today);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "UPDATE fees SET ClassID = ?, FeeName = ?, Amount = ? WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "Tuition Fee");
                        command.Parameters.AddWithValue("?", amount);
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // payment_record has no FeeName column — omit it from UPDATE.
                    var query = "UPDATE payment_record SET classID = ?, Balance = ?, student_name = ? WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", balance);
                        command.Parameters.AddWithValue("?", studentName);
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();
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
                using (var connection = new OleDbConnection(_connectionString))
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
                        query += TenantContext.FilterClause();
                    }

                    query += " ORDER BY [Date] DESC, [tm] DESC, [Balance] ASC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT TOP 1 [Amount] FROM [fees] WHERE [StudentID] = ? AND [ClassID] = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // payment_record has no FeeName column — omit it from INSERT.
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, PaymentRecordTable);
                    var query = tenant
                        ? "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name, SchoolId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"
                        : "INSERT INTO payment_record (StudentID, classID, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", newBalance);
                        command.Parameters.AddWithValue("?", studentName);
                        command.Parameters.AddWithValue("?", amountPaid);
                        command.Parameters.AddWithValue("?", date);
                        command.Parameters.AddWithValue("?", DateTime.Now.ToString("HH:mm:ss"));
                        command.Parameters.AddWithValue("?", paymentMode);
                        command.Parameters.AddWithValue("?", bursarName);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Actual column name in Students table is GuidianceEmail (legacy typo)
                    var query = "SELECT GuidianceEmail FROM Students WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, StudentsTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private async Task<string> GetStudentGuardianPhoneAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Column EmergencyConatct is a legacy typo; same number used at registration.
                    var query = "SELECT EmergencyConatct FROM Students WHERE StudentID = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, StudentsTable);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        public async Task<DataTable> GetPaymentHistoryTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
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
                        query += TenantContext.FilterClause();
                    }

                    query += " ORDER BY [Date] DESC, [tm] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
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

        public async Task<DataTable> GetOutstandingBalancesTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
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
                        query += TenantContext.FilterClause("p");
                    }

                    query += @"
                        ) latest
                        WHERE latest.rn = 1 AND latest.[Balance Owed] > 0
                        ORDER BY latest.[Balance Owed] DESC";
                    
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
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
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, FeesTable);
                    var query = "SELECT FeeID AS [FEE ID], StudentID AS [STUDENT ID], ClassID AS [CLASS ID], FeeName AS [FEE NAME], Amount AS [AMOUNT] FROM fees WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    query += " ORDER BY FeeID DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
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
    }
}

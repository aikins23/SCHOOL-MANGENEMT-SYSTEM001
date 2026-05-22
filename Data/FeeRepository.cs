using System;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class FeeRepository : IFeeRepository
    {
        private readonly string _connectionString;

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
                    var query = "INSERT INTO fees (StudentID, ClassID, FeeName, Amount) VALUES (?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "Tuition Fee");
                        command.Parameters.AddWithValue("?", amount);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    var query = "INSERT INTO payment_record (StudentID, classID, FeeName, Balance, student_name, Amount_paid, [Date]) VALUES (?, ?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "SCHOOLFEES");
                        command.Parameters.AddWithValue("?", balance);
                        command.Parameters.AddWithValue("?", studentName);
                        command.Parameters.AddWithValue("?", 0m);
                        command.Parameters.AddWithValue("?", DateTime.Today);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "Tuition Fee");
                        command.Parameters.AddWithValue("?", amount);
                        command.Parameters.AddWithValue("?", studentId);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    var query = "UPDATE payment_record SET classID = ?, FeeName = ?, Balance = ?, student_name = ? WHERE StudentID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "SCHOOLFEES");
                        command.Parameters.AddWithValue("?", balance);
                        command.Parameters.AddWithValue("?", studentName);
                        command.Parameters.AddWithValue("?", studentId);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    var query = "SELECT TOP 1 [Balance] FROM [payment_record] WHERE [StudentID] = ? ORDER BY [Date] DESC, [tm] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
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
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
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
                    var query = "INSERT INTO payment_record (StudentID, classID, FeeName, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        command.Parameters.AddWithValue("?", classId);
                        command.Parameters.AddWithValue("?", "School Fees");
                        command.Parameters.AddWithValue("?", newBalance);
                        command.Parameters.AddWithValue("?", studentName);
                        command.Parameters.AddWithValue("?", amountPaid);
                        command.Parameters.AddWithValue("?", date);
                        command.Parameters.AddWithValue("?", DateTime.Now.ToString("HH:mm:ss"));
                        command.Parameters.AddWithValue("?", paymentMode);
                        command.Parameters.AddWithValue("?", bursarName);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error adding payment record", ex);
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
                        ORDER BY [Date] DESC, [tm] DESC";
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
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
                    var query = @"
                        SELECT 
                            p.StudentID AS [ID], 
                            p.student_name AS [Student Name], 
                            p.classID AS [Class], 
                            p.Balance AS [Balance Owed],
                            p.[Date] AS [Last Payment]
                        FROM payment_record p
                        INNER JOIN (
                            SELECT StudentID, MAX([Date]) as LastDate, MAX(tm) as LastTime
                            FROM payment_record
                            GROUP BY StudentID
                        ) latest ON p.StudentID = latest.StudentID AND p.[Date] = latest.LastDate AND p.tm = latest.LastTime
                        WHERE p.Balance > 0
                        ORDER BY p.Balance DESC";
                    
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
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
                    var query = "SELECT FeeID AS [FEE ID], StudentID AS [STUDENT ID], ClassID AS [CLASS ID], FeeName AS [FEE NAME], Amount AS [AMOUNT] FROM fees ORDER BY FeeID DESC";
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving fees table", ex);
            }
            return table;
        }
    }
}

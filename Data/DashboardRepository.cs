using System;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<int> GetStudentCountAsync()
        {
            return await ExecuteScalarIntAsync("SELECT COUNT(*) FROM Students");
        }

        public async Task<int> GetEmployeeCountAsync()
        {
            return await ExecuteScalarIntAsync("SELECT COUNT(*) FROM Employee");
        }

        public async Task<int> GetPendingLeaveCountAsync()
        {
            return await ExecuteScalarIntAsync("SELECT COUNT(*) FROM emp_leave WHERE UPPER([status]) = 'PENDING'");
        }

        public async Task<decimal> GetTotalFeesCollectedAsync()
        {
            return await ExecuteScalarDecimalAsync("SELECT SUM(Amount_paid) FROM payment_record");
        }

        public async Task<decimal> GetTotalFeesBalanceAsync()
        {
            return await ExecuteScalarDecimalAsync("SELECT SUM(Balance) FROM payment_record WHERE Balance > 0");
        }

        public async Task<decimal> GetAverageExamScoreAsync()
        {
            return await ExecuteScalarDecimalAsync("SELECT AVG(gt) FROM examss");
        }

        public async Task<string> GetTopClassByEnrollmentAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT TOP 1 ClassID FROM Students GROUP BY ClassID ORDER BY COUNT(*) DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "No data";
                    }
                }
            }
            catch { return "No data"; }
        }

        public async Task<DataTable> GetRecentPaymentsAsync(int count)
        {
            var query = $@"
                SELECT TOP {count} 
                    StudentID AS [ID], 
                    student_name AS [Student], 
                    classID AS [Class], 
                    Amount_paid AS [Paid], 
                    Balance, 
                    [Date], 
                    payment_mode AS [Mode], 
                    Bursor_name AS [Bursar] 
                FROM payment_record 
                ORDER BY [Date] DESC, tm DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetClassEnrollmentSummaryAsync()
        {
            var query = "SELECT ClassID AS [Class], COUNT(*) AS [Students] FROM Students GROUP BY ClassID ORDER BY ClassID";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetLeaveStatusSummaryAsync()
        {
            var query = "SELECT [status] AS [Status], COUNT(*) AS [Total] FROM emp_leave GROUP BY [status] ORDER BY [status]";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetAverageScoreBySubjectAsync()
        {
            var query = "SELECT [subject], AVG(gt) AS AvgScore FROM examss GROUP BY [subject] ORDER BY AVG(gt) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetMonthlyFeeCollectionTrendAsync(int year)
        {
            var query = $@"
                SELECT MONTH([Date]) AS Mo, SUM(Amount_paid) AS Total 
                FROM payment_record 
                WHERE YEAR([Date]) = ? 
                GROUP BY MONTH([Date]) 
                ORDER BY MONTH([Date])";
            
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", year);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch { }
            return table;
        }

        private async Task<int> ExecuteScalarIntAsync(string query)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                    }
                }
            }
            catch { return 0; }
        }

        private async Task<decimal> ExecuteScalarDecimalAsync(string query)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                    }
                }
            }
            catch { return 0m; }
        }

        private async Task<DataTable> FetchTableAsync(string query)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch { /* Ignore or log */ }
            return table;
        }
    }
}

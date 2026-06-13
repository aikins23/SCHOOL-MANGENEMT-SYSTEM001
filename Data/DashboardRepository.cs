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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error getting top class by enrollment", ex);
                return "No data";
            }
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
            // Drives the grid off ClassAssignments so every configured class
            // appears — even classes that currently have zero students.
            // COUNT(s.StudentID) gives 0 for empty classes (COUNT(*) would give 1).
            var query = @"
                SELECT
                    ca.ClassName              AS [Class],
                    COUNT(s.StudentID)        AS [Enrollment],
                    ISNULL(e.fullName, '—')   AS [Class Teacher]
                FROM ClassAssignments ca
                LEFT JOIN Students s ON s.ClassID = ca.ClassName
                LEFT JOIN Employee e ON e.employmentID = ca.ClassTeacherID
                GROUP BY ca.ClassName, e.fullName
                ORDER BY
                    CASE ca.ClassName
                        WHEN 'CRECHE'         THEN 1
                        WHEN 'NURSERY 1'      THEN 2
                        WHEN 'NURSERY 2'      THEN 3
                        WHEN 'KINDERGARTEN 1' THEN 4
                        WHEN 'KINDERGARTEN 2' THEN 5
                        WHEN 'BASIC 1'        THEN 6
                        WHEN 'BASIC 2'        THEN 7
                        WHEN 'BASIC 3'        THEN 8
                        WHEN 'BASIC 4'        THEN 9
                        WHEN 'BASIC 5'        THEN 10
                        WHEN 'BASIC 6'        THEN 11
                        WHEN 'BASIC 7'        THEN 12
                        WHEN 'BASIC 8'        THEN 13
                        WHEN 'BASIC 9'        THEN 14
                        ELSE 99
                    END";
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error getting monthly fee collection trend for {year}", ex);
            }
            return table;
        }

        public async Task<DataTable> GetMonthlyAttendanceRateAsync(int year)
        {
            var query = @"
                SELECT MONTH([Date]) AS Mo,
                       CAST(SUM(CASE WHEN UPPER([Status]) = 'PRESENT' THEN 1 ELSE 0 END) * 100.0
                            / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) AS RatePct
                FROM Attendance
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error getting monthly attendance rate for {year}", ex);
            }
            return table;
        }

        public async Task<DataTable> GetMonthlyIncomeVsExpensesAsync(int year)
        {
            // Expenses.Amount is stored as varchar — strip commas before casting.
            var query = @"
                SELECT Mo, SUM(Income) AS Income, SUM(Expense) AS Expense FROM (
                    SELECT MONTH([Date]) AS Mo, SUM(Amount_paid) AS Income, 0 AS Expense
                    FROM payment_record WHERE YEAR([Date]) = ?
                    GROUP BY MONTH([Date])
                    UNION ALL
                    SELECT MONTH(Date_Time) AS Mo, 0 AS Income,
                           SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) AS Expense
                    FROM Expenses WHERE YEAR(Date_Time) = ?
                    GROUP BY MONTH(Date_Time)
                ) t
                GROUP BY Mo
                ORDER BY Mo";

            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", year);
                        command.Parameters.AddWithValue("?", year);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error getting monthly income vs expenses for {year}", ex);
            }
            return table;
        }

        public async Task<DataTable> GetGradeDistributionAsync()
        {
            var query = @"
                SELECT grade AS Grade, COUNT(*) AS Total
                FROM examss
                WHERE grade IS NOT NULL AND LTRIM(RTRIM(grade)) <> ''
                GROUP BY grade
                ORDER BY grade";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetAttendanceRateByClassAsync()
        {
            var query = @"
                SELECT s.ClassID AS [Class],
                       CAST(SUM(CASE WHEN UPPER(a.[Status]) = 'PRESENT' THEN 1 ELSE 0 END) * 100.0
                            / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) AS RatePct
                FROM Attendance a
                INNER JOIN Students s ON s.StudentID = a.ReferenceID
                WHERE UPPER(a.ReferenceType) = 'STUDENT'
                GROUP BY s.ClassID
                ORDER BY s.ClassID";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetOutstandingFeesByClassAsync()
        {
            // Use the latest payment row per student to avoid summing historical balances.
            var query = @"
                SELECT classID AS [Class], SUM(Balance) AS Outstanding
                FROM (
                    SELECT pr.StudentID, pr.classID, pr.Balance,
                           ROW_NUMBER() OVER (PARTITION BY pr.StudentID ORDER BY pr.[Date] DESC, pr.tm DESC) AS rn
                    FROM payment_record pr
                ) latest
                WHERE rn = 1 AND Balance > 0
                GROUP BY classID
                ORDER BY SUM(Balance) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetPaymentModeBreakdownAsync()
        {
            var query = @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(payment_mode)), ''), 'Unknown') AS Mode,
                       SUM(Amount_paid) AS Total
                FROM payment_record
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(payment_mode)), ''), 'Unknown')
                ORDER BY SUM(Amount_paid) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetStaffByDepartmentAsync()
        {
            var query = @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned') AS Department,
                       COUNT(*) AS Total
                FROM Employee
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned')
                ORDER BY COUNT(*) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetExpenseByCategoryAsync()
        {
            var query = @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(Purpose)), ''), 'Uncategorized') AS Category,
                       SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) AS Total
                FROM Expenses
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(Purpose)), ''), 'Uncategorized')
                ORDER BY SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetTopAbsentStudentsAsync(int topN)
        {
            var query = $@"
                SELECT TOP {topN} a.FullName AS Student,
                       SUM(CASE WHEN UPPER(a.[Status]) = 'ABSENT' THEN 1 ELSE 0 END) AS Absences
                FROM Attendance a
                WHERE UPPER(a.ReferenceType) = 'STUDENT'
                GROUP BY a.FullName
                HAVING SUM(CASE WHEN UPPER(a.[Status]) = 'ABSENT' THEN 1 ELSE 0 END) > 0
                ORDER BY SUM(CASE WHEN UPPER(a.[Status]) = 'ABSENT' THEN 1 ELSE 0 END) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetClassAverageScoreAsync()
        {
            var query = @"
                SELECT std_class AS [Class], AVG(gt) AS AvgScore
                FROM examss
                WHERE std_class IS NOT NULL AND LTRIM(RTRIM(std_class)) <> ''
                GROUP BY std_class
                ORDER BY std_class";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetStudentGenderDistributionAsync()
        {
            var query = @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(Gender)), ''), 'Unknown') AS Gender,
                       COUNT(*) AS Total
                FROM Students
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(Gender)), ''), 'Unknown')
                ORDER BY COUNT(*) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetTermOverTermPerformanceAsync()
        {
            var query = @"
                SELECT [year] AS Yr, term AS Term, AVG(gt) AS AvgScore
                FROM examss
                WHERE gt IS NOT NULL
                  AND [year] IS NOT NULL AND LTRIM(RTRIM([year])) <> ''
                  AND term IS NOT NULL AND LTRIM(RTRIM(term)) <> ''
                GROUP BY [year], term
                ORDER BY [year], term";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetAdmissionsPerYearAsync()
        {
            var query = @"
                SELECT YEAR(admission_date) AS Yr, COUNT(*) AS Total
                FROM Students
                WHERE admission_date IS NOT NULL
                GROUP BY YEAR(admission_date)
                ORDER BY YEAR(admission_date)";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetActiveVsRolledOutStudentsAsync()
        {
            var query = @"
                SELECT 'Active' AS Bucket, COUNT(*) AS Total FROM Students
                UNION ALL
                SELECT 'Rolled Out' AS Bucket, COUNT(*) AS Total FROM Rolled_Out_Students";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetSalarySpendByDepartmentAsync()
        {
            var query = @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned') AS Department,
                       SUM(salary) AS TotalSalary
                FROM Employee
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned')
                ORDER BY SUM(salary) DESC";
            return await FetchTableAsync(query);
        }

        public async Task<DataTable> GetSubjectPassFailRateAsync()
        {
            // Pass threshold: gt >= 50.
            var query = @"
                SELECT [subject] AS Subject,
                       SUM(CASE WHEN gt >= 50 THEN 1 ELSE 0 END) AS PassCount,
                       SUM(CASE WHEN gt <  50 THEN 1 ELSE 0 END) AS FailCount
                FROM examss
                WHERE gt IS NOT NULL
                  AND [subject] IS NOT NULL AND LTRIM(RTRIM([subject])) <> ''
                GROUP BY [subject]
                ORDER BY [subject]";
            return await FetchTableAsync(query);
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error executing scalar int query: {query}", ex);
                return 0;
            }
        }

        public async Task<decimal> GetTotalIncomeBetweenAsync(DateTime from, DateTime to) =>
            await ScalarDecimalRangeAsync(
                "SELECT ISNULL(SUM(Amount_paid),0) FROM payment_record WHERE [Date] BETWEEN ? AND ?", from, to);

        public async Task<decimal> GetTotalExpensesBetweenAsync(DateTime from, DateTime to) =>
            await ScalarDecimalRangeAsync(
                "SELECT ISNULL(SUM(TRY_CAST(REPLACE(ISNULL(Amount,'0'),',','') AS DECIMAL(18,2))),0) FROM Expenses WHERE Date_Time BETWEEN ? AND ?", from, to);

        private async Task<decimal> ScalarDecimalRangeAsync(string query, DateTime from, DateTime to)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", from.Date);
                        command.Parameters.AddWithValue("?", to.Date);
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                    }
                }
            }
            catch { return 0m; }
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error executing scalar decimal query: {query}", ex);
                return 0m;
            }
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
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error fetching table with query: {query}", ex);
            }
            return table;
        }
    }
}

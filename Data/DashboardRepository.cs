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

        public async Task<DashboardCoreMetrics> GetCoreMetricsAsync()
        {
            var m = new DashboardCoreMetrics { TopClass = "No data", TopExpenseCategory = "—", LargestExpenseItem = "—" };
            
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Basic counts and averages
                    var studentsTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                    var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Employee");
                    var leaveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "emp_leave");
                    var paymentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "payment_record");
                    var examsTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "examss");
                    var basicQuery = $@"
                        SELECT
                            (SELECT COUNT(*) FROM Students WHERE 1=1 {(studentsTenant ? TenantContext.FilterClause() : "")}) AS StudentCount,
                            (SELECT COUNT(*) FROM Employee WHERE 1=1 {(employeeTenant ? TenantContext.FilterClause() : "")}) AS EmployeeCount,
                            (SELECT COUNT(*) FROM emp_leave WHERE UPPER([status]) = 'PENDING' {(leaveTenant ? TenantContext.FilterClause() : "")}) AS PendingLeaveCount,
                            (SELECT ISNULL(SUM(Amount_paid), 0) FROM payment_record WHERE 1=1 {(paymentTenant ? TenantContext.FilterClause() : "")}) AS TotalFeesCollected,
                            (SELECT ISNULL(SUM(Balance), 0) FROM payment_record WHERE Balance > 0 {(paymentTenant ? TenantContext.FilterClause() : "")}) AS TotalFeesBalance,
                            (SELECT ISNULL(AVG(gt), 0) FROM examss WHERE 1=1 {(examsTenant ? TenantContext.FilterClause() : "")}) AS AverageExamScore,
                            ISNULL((SELECT TOP 1 ClassID FROM Students WHERE 1=1 {(studentsTenant ? TenantContext.FilterClause() : "")} GROUP BY ClassID ORDER BY COUNT(*) DESC), 'No data') AS TopClass";

                    using (var cmd = new OleDbCommand(basicQuery, connection))
                    {
                        if (studentsTenant) TenantContext.AddSchoolParameter(cmd);
                        if (employeeTenant) TenantContext.AddSchoolParameter(cmd);
                        if (leaveTenant) TenantContext.AddSchoolParameter(cmd);
                        if (paymentTenant) TenantContext.AddSchoolParameter(cmd);
                        if (paymentTenant) TenantContext.AddSchoolParameter(cmd);
                        if (examsTenant) TenantContext.AddSchoolParameter(cmd);
                        if (studentsTenant) TenantContext.AddSchoolParameter(cmd);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                m.StudentCount = AsInt(reader["StudentCount"]);
                                m.EmployeeCount = AsInt(reader["EmployeeCount"]);
                                m.PendingLeaveCount = AsInt(reader["PendingLeaveCount"]);
                                m.TotalFeesCollected = AsDecimal(reader["TotalFeesCollected"]);
                                m.TotalFeesBalance = AsDecimal(reader["TotalFeesBalance"]);
                                m.AverageExamScore = AsDecimal(reader["AverageExamScore"]);
                                m.TopClass = reader["TopClass"]?.ToString() ?? "No data";
                            }
                        }
                    }

                    // 2. Expense metrics (separate to avoid failing the whole dashboard if Expenses table has issues)
                    try
                    {
                        const string expenseQuery = @"
                            SELECT
                                ISNULL((
                                    SELECT TOP 1 Purpose
                                    FROM Expenses
                                    GROUP BY Purpose
                                    ORDER BY SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) DESC
                                ), '—') AS TopExpenseCategory,
                                ISNULL((
                                    SELECT TOP 1 SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2)))
                                    FROM Expenses
                                    GROUP BY Purpose
                                    ORDER BY SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) DESC
                                ), 0) AS TopExpenseAmount,
                                ISNULL((
                                    SELECT TOP 1 Expenses_name
                                    FROM Expenses
                                    ORDER BY TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2)) DESC
                                ), '—') AS LargestExpenseItem,
                                ISNULL((
                                    SELECT TOP 1 TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))
                                    FROM Expenses
                                    ORDER BY TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2)) DESC
                                ), 0) AS LargestExpenseAmount";

                        using (var cmd = new OleDbCommand(expenseQuery, connection))
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                m.TopExpenseCategory = reader["TopExpenseCategory"]?.ToString() ?? "—";
                                m.TopExpenseAmount = AsDecimal(reader["TopExpenseAmount"]);
                                m.LargestExpenseItem = reader["LargestExpenseItem"]?.ToString() ?? "—";
                                m.LargestExpenseAmount = AsDecimal(reader["LargestExpenseAmount"]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Services.LoggerHelper.LogWarning("Expenses metrics skipped: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error getting dashboard core metrics", ex);
            }

            return m;
        }

        public async Task<int> GetStudentCountAsync()
        {
            return await ExecuteTenantScalarIntAsync("Students", "SELECT COUNT(*) FROM Students");
        }

        public async Task<int> GetEmployeeCountAsync()
        {
            return await ExecuteTenantScalarIntAsync("Employee", "SELECT COUNT(*) FROM Employee");
        }

        public async Task<int> GetPendingLeaveCountAsync()
        {
            return await ExecuteTenantScalarIntAsync("emp_leave", "SELECT COUNT(*) FROM emp_leave WHERE UPPER([status]) = 'PENDING'");
        }

        public async Task<decimal> GetTotalFeesCollectedAsync()
        {
            return await ExecuteTenantScalarDecimalAsync("payment_record", "SELECT SUM(Amount_paid) FROM payment_record");
        }

        public async Task<decimal> GetTotalFeesBalanceAsync()
        {
            return await ExecuteTenantScalarDecimalAsync("payment_record", "SELECT SUM(Balance) FROM payment_record WHERE Balance > 0");
        }

        public async Task<decimal> GetAverageExamScoreAsync()
        {
            return await ExecuteTenantScalarDecimalAsync("examss", "SELECT AVG(gt) FROM examss");
        }

        public async Task<string> GetTopClassByEnrollmentAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                    var query = "SELECT TOP 1 ClassID FROM Students WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    query += " GROUP BY ClassID ORDER BY COUNT(*) DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "payment_record");
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
                        WHERE 1=1";
                    if (tenant) query += TenantContext.FilterClause();
                    query += " ORDER BY [Date] DESC, tm DESC";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error getting recent payments", ex);
            }

            return table;
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
            return await FetchTenantTableAsync("emp_leave", tenant =>
                "SELECT [status] AS [Status], COUNT(*) AS [Total] FROM emp_leave WHERE 1=1" +
                (tenant ? TenantContext.FilterClause() : "") +
                " GROUP BY [status] ORDER BY [status]");
        }

        public async Task<DataTable> GetAverageScoreBySubjectAsync()
        {
            return await FetchTenantTableAsync("examss", tenant =>
                "SELECT [subject], AVG(gt) AS AvgScore FROM examss WHERE 1=1" +
                (tenant ? TenantContext.FilterClause() : "") +
                " GROUP BY [subject] ORDER BY AVG(gt) DESC");
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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "payment_record");
                    if (tenant)
                    {
                        query = query.Replace("WHERE YEAR([Date]) = ?", "WHERE YEAR([Date]) = ?" + TenantContext.FilterClause());
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", year);
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
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Attendance");
                    if (tenant)
                    {
                        query = query.Replace("WHERE YEAR([Date]) = ?", "WHERE YEAR([Date]) = ?" + TenantContext.FilterClause());
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", year);
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
                    var paymentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "payment_record");
                    var expensesTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Expenses");
                    if (paymentTenant)
                    {
                        query = query.Replace("FROM payment_record WHERE YEAR([Date]) = ?", "FROM payment_record WHERE YEAR([Date]) = ?" + TenantContext.FilterClause());
                    }

                    if (expensesTenant)
                    {
                        query = query.Replace("FROM Expenses WHERE YEAR(Date_Time) = ?", "FROM Expenses WHERE YEAR(Date_Time) = ?" + TenantContext.FilterClause());
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", year);
                        if (paymentTenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        command.Parameters.AddWithValue("?", year);
                        if (expensesTenant)
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
                Services.LoggerHelper.LogError($"Error getting monthly income vs expenses for {year}", ex);
            }
            return table;
        }

        public async Task<DataTable> GetGradeDistributionAsync()
        {
            return await FetchTenantTableAsync("examss", tenant => @"
                SELECT grade AS Grade, COUNT(*) AS Total
                FROM examss
                WHERE grade IS NOT NULL AND LTRIM(RTRIM(grade)) <> ''" +
                (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY grade
                ORDER BY grade");
        }

        public async Task<DataTable> GetAttendanceRateByClassAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var attendanceTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Attendance");
                    var studentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                    var query = @"
                        SELECT s.ClassID AS [Class],
                               CAST(SUM(CASE WHEN UPPER(a.[Status]) = 'PRESENT' THEN 1 ELSE 0 END) * 100.0
                                    / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) AS RatePct
                        FROM Attendance a
                        INNER JOIN Students s ON s.StudentID = a.ReferenceID
                        WHERE UPPER(a.ReferenceType) = 'STUDENT'";
                    if (attendanceTenant) query += TenantContext.FilterClause("a");
                    if (studentTenant) query += TenantContext.FilterClause("s");
                    query += " GROUP BY s.ClassID ORDER BY s.ClassID";

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (attendanceTenant) TenantContext.AddSchoolParameter(command);
                        if (studentTenant) TenantContext.AddSchoolParameter(command);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error getting attendance rate by class", ex);
            }

            return table;
        }

        public async Task<DataTable> GetOutstandingFeesByClassAsync()
        {
            // Use the latest payment row per student to avoid summing historical balances.
            return await FetchTenantTableAsync("payment_record", tenant => @"
                SELECT classID AS [Class], SUM(Balance) AS Outstanding
                FROM (
                    SELECT pr.StudentID, pr.classID, pr.Balance,
                           ROW_NUMBER() OVER (PARTITION BY pr.StudentID ORDER BY pr.[Date] DESC, pr.tm DESC) AS rn
                    FROM payment_record pr
                    WHERE 1=1" + (tenant ? TenantContext.FilterClause("pr") : "") + @"
                ) latest
                WHERE rn = 1 AND Balance > 0
                GROUP BY classID
                ORDER BY SUM(Balance) DESC");
        }

        public async Task<DataTable> GetPaymentModeBreakdownAsync()
        {
            return await FetchTenantTableAsync("payment_record", tenant => @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(payment_mode)), ''), 'Unknown') AS Mode,
                       SUM(Amount_paid) AS Total
                FROM payment_record
                WHERE 1=1" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(payment_mode)), ''), 'Unknown')
                ORDER BY SUM(Amount_paid) DESC");
        }

        public async Task<DataTable> GetStaffByDepartmentAsync()
        {
            return await FetchTenantTableAsync("Employee", tenant => @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned') AS Department,
                       COUNT(*) AS Total
                FROM Employee
                WHERE 1=1" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned')
                ORDER BY COUNT(*) DESC");
        }

        public async Task<DataTable> GetExpenseByCategoryAsync()
        {
            return await FetchTenantTableAsync("Expenses", tenant => @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(Purpose)), ''), 'Uncategorized') AS Category,
                       SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) AS Total
                FROM Expenses
                WHERE 1=1" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(Purpose)), ''), 'Uncategorized')
                ORDER BY SUM(TRY_CAST(REPLACE(ISNULL(Amount, '0'), ',', '') AS DECIMAL(18,2))) DESC");
        }

        public async Task<DataTable> GetTopAbsentStudentsAsync(int topN)
        {
            return await FetchTenantTableAsync("Attendance", tenant => $@"
                SELECT TOP {topN} a.FullName AS Student,
                       SUM(CASE WHEN UPPER(a.[Status]) = 'ABSENT' THEN 1 ELSE 0 END) AS Absences
                FROM Attendance a
                WHERE UPPER(a.ReferenceType) = 'STUDENT'" + (tenant ? TenantContext.FilterClause("a") : "") + @"
                GROUP BY a.FullName
                HAVING SUM(CASE WHEN UPPER(a.[Status]) = 'ABSENT' THEN 1 ELSE 0 END) > 0
                ORDER BY SUM(CASE WHEN UPPER(a.[Status]) = 'ABSENT' THEN 1 ELSE 0 END) DESC");
        }

        public async Task<DataTable> GetClassAverageScoreAsync()
        {
            return await FetchTenantTableAsync("examss", tenant => @"
                SELECT std_class AS [Class], AVG(gt) AS AvgScore
                FROM examss
                WHERE std_class IS NOT NULL AND LTRIM(RTRIM(std_class)) <> ''" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY std_class
                ORDER BY std_class");
        }

        public async Task<DataTable> GetStudentGenderDistributionAsync()
        {
            return await FetchTenantTableAsync("Students", tenant => @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(Gender)), ''), 'Unknown') AS Gender,
                       COUNT(*) AS Total
                FROM Students
                WHERE 1=1" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(Gender)), ''), 'Unknown')
                ORDER BY COUNT(*) DESC");
        }

        public async Task<DataTable> GetTermOverTermPerformanceAsync()
        {
            return await FetchTenantTableAsync("examss", tenant => @"
                SELECT [year] AS Yr, term AS Term, AVG(gt) AS AvgScore
                FROM examss
                WHERE gt IS NOT NULL
                  AND [year] IS NOT NULL AND LTRIM(RTRIM([year])) <> ''
                  AND term IS NOT NULL AND LTRIM(RTRIM(term)) <> ''" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY [year], term
                ORDER BY [year], term");
        }

        public async Task<DataTable> GetAdmissionsPerYearAsync()
        {
            return await FetchTenantTableAsync("Students", tenant => @"
                SELECT YEAR(admission_date) AS Yr, COUNT(*) AS Total
                FROM Students
                WHERE admission_date IS NOT NULL" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY YEAR(admission_date)
                ORDER BY YEAR(admission_date)");
        }

        public async Task<DataTable> GetActiveVsRolledOutStudentsAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var studentTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Students");
                    var archiveTenant = await TenantContext.HasSchoolIdColumnAsync(connection, "Rolled_Out_Students");
                    var query = @"
                        SELECT 'Active' AS Bucket, COUNT(*) AS Total FROM Students WHERE 1=1";
                    if (studentTenant) query += TenantContext.FilterClause();
                    query += @"
                        UNION ALL
                        SELECT 'Rolled Out' AS Bucket, COUNT(*) AS Total FROM Rolled_Out_Students WHERE 1=1";
                    if (archiveTenant) query += TenantContext.FilterClause();

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (studentTenant) TenantContext.AddSchoolParameter(command);
                        if (archiveTenant) TenantContext.AddSchoolParameter(command);
                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error getting active vs rolled out students", ex);
            }

            return table;
        }

        public async Task<DataTable> GetSalarySpendByDepartmentAsync()
        {
            return await FetchTenantTableAsync("Employee", tenant => @"
                SELECT ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned') AS Department,
                       SUM(salary) AS TotalSalary
                FROM Employee
                WHERE 1=1" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(department)), ''), 'Unassigned')
                ORDER BY SUM(salary) DESC");
        }

        public async Task<DataTable> GetSubjectPassFailRateAsync()
        {
            // Pass threshold: gt >= 50.
            return await FetchTenantTableAsync("examss", tenant => @"
                SELECT [subject] AS Subject,
                       SUM(CASE WHEN gt >= 50 THEN 1 ELSE 0 END) AS PassCount,
                       SUM(CASE WHEN gt <  50 THEN 1 ELSE 0 END) AS FailCount
                FROM examss
                WHERE gt IS NOT NULL
                  AND [subject] IS NOT NULL AND LTRIM(RTRIM([subject])) <> ''" + (tenant ? TenantContext.FilterClause() : "") + @"
                GROUP BY [subject]
                ORDER BY [subject]");
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

        private async Task<int> ExecuteTenantScalarIntAsync(string tableName, string query)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, tableName);
                    if (tenant)
                    {
                        query += query.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase) >= 0
                            ? TenantContext.FilterClause()
                            : " WHERE 1=1" + TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error executing tenant scalar int query: {query}", ex);
                return 0;
            }
        }

        public async Task<decimal> GetTotalIncomeBetweenAsync(DateTime from, DateTime to) =>
            await ScalarDecimalRangeAsync(
                "payment_record",
                "SELECT ISNULL(SUM(Amount_paid),0) FROM payment_record WHERE [Date] BETWEEN ? AND ?", from, to);

        public async Task<decimal> GetTotalExpensesBetweenAsync(DateTime from, DateTime to) =>
            await ScalarDecimalRangeAsync(
                "Expenses",
                "SELECT ISNULL(SUM(TRY_CAST(REPLACE(ISNULL(Amount,'0'),',','') AS DECIMAL(18,2))),0) FROM Expenses WHERE Date_Time BETWEEN ? AND ?", from, to);

        private async Task<decimal> ScalarDecimalRangeAsync(string tableName, string query, DateTime from, DateTime to)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, tableName);
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", from.Date);
                        command.Parameters.AddWithValue("?", to.Date);
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

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

        private async Task<decimal> ExecuteTenantScalarDecimalAsync(string tableName, string query)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, tableName);
                    if (tenant)
                    {
                        query += query.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase) >= 0
                            ? TenantContext.FilterClause()
                            : " WHERE 1=1" + TenantContext.FilterClause();
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error executing tenant scalar decimal query: {query}", ex);
                return 0m;
            }
        }

        private async Task<DataTable> FetchTableAsync(string query, params object[] parameters)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue("?", parameter ?? DBNull.Value);
                            }
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
                Services.LoggerHelper.LogError($"Error fetching table with query: {query}", ex);
            }
            return table;
        }

        private async Task<DataTable> FetchTenantTableAsync(string tableName, Func<bool, string> buildQuery, int tenantParameterCount = 1)
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, tableName);
                    var query = buildQuery(tenant);
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            for (int i = 0; i < tenantParameterCount; i++)
                            {
                                TenantContext.AddSchoolParameter(command);
                            }
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
                Services.LoggerHelper.LogError($"Error fetching tenant table for {tableName}", ex);
            }
            return table;
        }

        private static int AsInt(object value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static decimal AsDecimal(object value)
        {
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }
    }
}

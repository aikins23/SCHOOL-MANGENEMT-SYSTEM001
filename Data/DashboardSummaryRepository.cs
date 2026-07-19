using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class DashboardSummaryRepository
    {
        private readonly string _connectionString;

        public DashboardSummaryRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string sql = @"
IF OBJECT_ID(N'DashboardMonthlySummary', N'U') IS NULL
BEGIN
    CREATE TABLE DashboardMonthlySummary (
        SummaryMonth DATE NOT NULL PRIMARY KEY,
        FeeCollected DECIMAL(18,2) NOT NULL CONSTRAINT DF_DashboardMonthlySummary_FeeCollected DEFAULT 0,
        FeeBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_DashboardMonthlySummary_FeeBalance DEFAULT 0,
        ExpenseTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_DashboardMonthlySummary_ExpenseTotal DEFAULT 0,
        AttendancePresent INT NOT NULL CONSTRAINT DF_DashboardMonthlySummary_AttendancePresent DEFAULT 0,
        AttendanceTotal INT NOT NULL CONSTRAINT DF_DashboardMonthlySummary_AttendanceTotal DEFAULT 0,
        AttendanceRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_DashboardMonthlySummary_AttendanceRate DEFAULT 0,
        ExamAverage DECIMAL(5,2) NOT NULL CONSTRAINT DF_DashboardMonthlySummary_ExamAverage DEFAULT 0,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_DashboardMonthlySummary_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DashboardMonthlySummary_Month' AND object_id = OBJECT_ID(N'DashboardMonthlySummary'))
    CREATE INDEX IX_DashboardMonthlySummary_Month ON DashboardMonthlySummary(SummaryMonth);";

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.CommandTimeout = 60;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task RefreshMonthAsync(DateTime date)
        {
            await EnsureTablesAsync();

            var month = new DateTime(date.Year, date.Month, 1);
            var nextMonth = month.AddMonths(1);

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var paymentTenant = await TenantContext.HasSchoolIdColumnAsync(c, "payment_record");
                var expenseTenant = await TenantContext.HasSchoolIdColumnAsync(c, "Expenses");
                var attendanceTenant = await TenantContext.HasSchoolIdColumnAsync(c, "Attendance");
                var examTenant = await TenantContext.HasSchoolIdColumnAsync(c, "examss");

                decimal feeCollected = await ScalarDecimalAsync(c,
                    "payment_record",
                    "SELECT ISNULL(SUM(Amount_paid),0) FROM payment_record WHERE [Date] >= @From AND [Date] < @To",
                    paymentTenant, month, nextMonth);
                decimal feeBalance = await ScalarDecimalAsync(c,
                    "payment_record",
                    "SELECT ISNULL(SUM(Balance),0) FROM payment_record WHERE Balance > 0 AND [Date] >= @From AND [Date] < @To",
                    paymentTenant, month, nextMonth);
                decimal expenseTotal = await ScalarDecimalAsync(c,
                    "Expenses",
                    "SELECT ISNULL(SUM(TRY_CAST(REPLACE(ISNULL(Amount,'0'),',','') AS DECIMAL(18,2))),0) FROM Expenses WHERE Date_Time >= @From AND Date_Time < @To",
                    expenseTenant, month, nextMonth);
                decimal examAverage = await ScalarDecimalAsync(c,
                    "examss",
                    "SELECT ISNULL(AVG(gt),0) FROM examss WHERE TRY_CONVERT(INT, [year]) = @Year",
                    examTenant, month, nextMonth, date.Year);

                int attendancePresent = await ScalarIntAsync(c,
                    "Attendance",
                    "SELECT COUNT(*) FROM Attendance WHERE [Date] >= @From AND [Date] < @To AND UPPER([Status]) = 'PRESENT'",
                    attendanceTenant, month, nextMonth);
                int attendanceTotal = await ScalarIntAsync(c,
                    "Attendance",
                    "SELECT COUNT(*) FROM Attendance WHERE [Date] >= @From AND [Date] < @To",
                    attendanceTenant, month, nextMonth);
                decimal attendanceRate = attendanceTotal == 0
                    ? 0m
                    : Math.Round((attendancePresent * 100m) / attendanceTotal, 2);

                const string upsert = @"
MERGE DashboardMonthlySummary AS target
USING (SELECT @SummaryMonth AS SummaryMonth) AS source
ON target.SummaryMonth = source.SummaryMonth
WHEN MATCHED THEN
    UPDATE SET
        FeeCollected = @FeeCollected,
        FeeBalance = @FeeBalance,
        ExpenseTotal = @ExpenseTotal,
        AttendancePresent = @AttendancePresent,
        AttendanceTotal = @AttendanceTotal,
        AttendanceRate = @AttendanceRate,
        ExamAverage = @ExamAverage,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SummaryMonth, FeeCollected, FeeBalance, ExpenseTotal, AttendancePresent, AttendanceTotal, AttendanceRate, ExamAverage)
    VALUES (@SummaryMonth, @FeeCollected, @FeeBalance, @ExpenseTotal, @AttendancePresent, @AttendanceTotal, @AttendanceRate, @ExamAverage);";

                using (var cmd = new SqlCommand(upsert, c))
                {
                    cmd.Parameters.AddWithValue("@SummaryMonth", month);
                    cmd.Parameters.AddWithValue("@FeeCollected", feeCollected);
                    cmd.Parameters.AddWithValue("@FeeBalance", feeBalance);
                    cmd.Parameters.AddWithValue("@ExpenseTotal", expenseTotal);
                    cmd.Parameters.AddWithValue("@AttendancePresent", attendancePresent);
                    cmd.Parameters.AddWithValue("@AttendanceTotal", attendanceTotal);
                    cmd.Parameters.AddWithValue("@AttendanceRate", attendanceRate);
                    cmd.Parameters.AddWithValue("@ExamAverage", examAverage);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task RefreshYearAsync(int year)
        {
            await EnsureTablesAsync();
            for (int month = 1; month <= 12; month++)
            {
                await RefreshMonthAsync(new DateTime(year, month, 1));
            }
        }

        public async Task<DataTable> GetMonthlySummaryAsync(int year)
        {
            await EnsureTablesAsync();
            var table = new DataTable();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            using (var cmd = new SqlCommand(@"
SELECT
    MONTH(SummaryMonth) AS Mo,
    FeeCollected,
    FeeBalance,
    ExpenseTotal,
    AttendanceRate,
    ExamAverage
FROM DashboardMonthlySummary
WHERE YEAR(SummaryMonth) = @Year
ORDER BY SummaryMonth", c))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                await c.OpenAsync();
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    await Task.Run(() => adapter.Fill(table));
                }
            }
            return table;
        }

        public static void RefreshMonthBestEffort(string connectionString, DateTime date)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await new DashboardSummaryRepository(connectionString).RefreshMonthAsync(date).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Services.LoggerHelper.LogWarning("Dashboard summary refresh skipped: " + ex.Message);
                }
            });
        }

        public static void RefreshYearBestEffort(string connectionString, int year)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await new DashboardSummaryRepository(connectionString).RefreshYearAsync(year).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Services.LoggerHelper.LogWarning("Dashboard summary yearly refresh skipped: " + ex.Message);
                }
            });
        }

        private static async Task<decimal> ScalarDecimalAsync(SqlConnection c, string table, string sql, bool tenant, DateTime from, DateTime to, int? year = null)
        {
            if (tenant) sql += TenantContext.FilterClauseSql();
            using (var cmd = new SqlCommand(sql, c))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                if (year.HasValue) cmd.Parameters.AddWithValue("@Year", year.Value);
                if (tenant) TenantContext.AddSchoolParameter(cmd);
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
            }
        }

        private static async Task<int> ScalarIntAsync(SqlConnection c, string table, string sql, bool tenant, DateTime from, DateTime to)
        {
            if (tenant) sql += TenantContext.FilterClauseSql();
            using (var cmd = new SqlCommand(sql, c))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                if (tenant) TenantContext.AddSchoolParameter(cmd);
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }
    }
}

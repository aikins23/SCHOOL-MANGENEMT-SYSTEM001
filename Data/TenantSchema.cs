using KingdomPrep.Shared.Models;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Adds a SchoolId tenant column to business tables. This is the first layer of
    /// data isolation for selling the system to multiple schools and for future web sync.
    /// Existing desktop databases are backfilled to the configured local school id.
    /// </summary>
    public static class TenantSchema
    {
        public static readonly string[] TenantTables =
        {
            "Students", "Employee", "Users",
            "fees", "payment_record", "examss", "Attendance", "emp_leave",
            "DraftAdmissions", "Rolled_Out_Students", "Rolled_Out_Employees",
            "Classes", "ClassAssignments", "ClassFees", "ClassSubjects",
            "GradingScheme", "StudentTermRemarks",
            "Expenses", "ExpenseCategories",
            "Buses", "BusRoutes", "StudentTransport", "TransportPayment", "TransportReminderLog",
            "Books", "BookLoans",
            "SmsOutbox",
            "AcademicCalendar", "SchoolHolidays", "TimePeriods", "SubjectAllocations", "TimetableEntries",
            "ScholarshipCategories", "StudentScholarships"
        };

        public static async Task EnsureTenantColumnsAsync()
        {
            await EnsureTenantColumnsAsync(AppConfig.ConnectionString);
        }

        public static async Task EnsureTenantColumnsAsync(string connectionString)
        {
            Guid schoolId = await EnsureLocalSchoolIdAsync(connectionString);

            try
            {
                using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(connectionString)))
                {
                    await c.OpenAsync();
                    foreach (var table in TenantTables)
                    {
                        try
                        {
                            await EnsureForTableAsync(c, table, schoolId);
                        }
                        catch (Exception ex)
                        {
                            Services.LoggerHelper.LogWarning($"TenantSchema[{table}]: {ex.Message}");
                        }
                    }

                    await EnsureTenantKeyIndexesAsync(c);
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("TenantSchema: " + ex.Message);
            }
        }

        private static async Task<Guid> EnsureLocalSchoolIdAsync(string connectionString)
        {
            var repo = new SchoolInfoRepository(connectionString);
            await repo.EnsureTablesAsync();
            var info = await repo.GetAsync();
            if (info.SchoolId == Guid.Empty)
            {
                info.SchoolId = Guid.NewGuid();
                await repo.SaveAsync(info);
            }
            return info.SchoolId;
        }

        private static async Task EnsureForTableAsync(SqlConnection c, string table, Guid schoolId)
        {
            string safeTable = table.Replace("]", "]]").Replace("'", "''");
            string indexName = ("IX_" + table + "_SchoolId").Replace("]", "").Replace("[", "");
            string defaultName = ("DF_" + table + "_SchoolId").Replace("]", "").Replace("[", "");
            string school = schoolId.ToString("D");

            string sql = $@"
IF OBJECT_ID(N'{safeTable}', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('{safeTable}','SchoolId') IS NULL
        EXEC('ALTER TABLE [{safeTable}] ADD SchoolId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [{defaultName}] DEFAULT (''{school}'')');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}' AND object_id = OBJECT_ID(N'{safeTable}'))
        EXEC('CREATE INDEX [{indexName}] ON [{safeTable}](SchoolId)');
END";
            using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureTenantKeyIndexesAsync(SqlConnection c)
        {
            string[] scripts =
            {
                UniqueWhenClean("Users", "UX_Users_School_Username", "SchoolId, Username", "Username IS NOT NULL"),
                UniqueWhenClean("Students", "UX_Students_School_StudentID", "SchoolId, StudentID", "StudentID IS NOT NULL"),
                UniqueWhenClean("Employee", "UX_Employee_School_EmploymentID", "SchoolId, employmentID", "employmentID IS NOT NULL"),
                UniqueWhenClean("Classes", "UX_Classes_School_ClassName", "SchoolId, ClassName", "ClassName IS NOT NULL"),
                UniqueWhenClean("ClassAssignments", "UX_ClassAssignments_School_ClassName", "SchoolId, ClassName", "ClassName IS NOT NULL"),
                UniqueWhenClean("ClassSubjects", "UX_ClassSubjects_School_ClassSubject", "SchoolId, ClassName, Subject", "ClassName IS NOT NULL AND Subject IS NOT NULL"),
                UniqueWhenClean("StudentTransport", "UX_StudentTransport_School_StudentID", "SchoolId, StudentID", "StudentID IS NOT NULL"),
                UniqueWhenClean("TransportReminderLog", "UX_TransportReminder_School_StudentPeriod", "SchoolId, StudentID, Period", "StudentID IS NOT NULL AND Period IS NOT NULL"),
                UniqueWhenClean("ScholarshipCategories", "UX_ScholarshipCategories_School_Name", "SchoolId, [Name]", "[Name] IS NOT NULL")
            };

            foreach (var sql in scripts)
            {
                try
                {
                    using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Services.LoggerHelper.LogWarning("TenantSchema key index skipped: " + ex.Message);
                }
            }
        }

        private static string UniqueWhenClean(string table, string indexName, string columns, string notNullFilter)
        {
            string safeTable = table.Replace("'", "''").Replace("]", "]]");
            string safeIndex = indexName.Replace("]", "").Replace("[", "");
            return $@"
IF OBJECT_ID(N'{safeTable}', N'U') IS NOT NULL
   AND COL_LENGTH('{safeTable}','SchoolId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{safeIndex}' AND object_id = OBJECT_ID(N'{safeTable}'))
   AND NOT EXISTS (
        SELECT 1
        FROM [{safeTable}]
        WHERE {notNullFilter}
        GROUP BY {columns}
        HAVING COUNT(*) > 1)
BEGIN
    EXEC('CREATE UNIQUE INDEX [{safeIndex}] ON [{safeTable}]({columns})');
END";
        }
    }
}

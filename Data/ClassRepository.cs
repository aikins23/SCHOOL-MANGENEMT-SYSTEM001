using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class ClassRepository : IClassRepository
    {
        private readonly string _connectionString;
        private const string CLASSES_TABLE = "Classes";
        private const string ASSIGNMENTS_TABLE = "ClassAssignments";

        public ClassRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task EnsureTableExistsAsync()
        {
            using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await connection.OpenAsync();

                const string createClasses = @"IF OBJECT_ID(N'Classes', N'U') IS NULL
                    CREATE TABLE Classes (
                        ClassName NVARCHAR(50) NOT NULL PRIMARY KEY,
                        TuitionFee MONEY NOT NULL CONSTRAINT DF_Classes_TuitionFee DEFAULT (0),
                        PromotionLevel INT NOT NULL CONSTRAINT DF_Classes_PromotionLevel DEFAULT (0));";
                using (var command = new SqlCommand(createClasses, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                const string createAssignments = @"IF OBJECT_ID(N'ClassAssignments', N'U') IS NULL
                    CREATE TABLE ClassAssignments (
                        ClassName NVARCHAR(50) NOT NULL PRIMARY KEY,
                        ClassTeacherID INT NULL,
                        AssignedDate DATETIME NULL);";
                using (var command = new SqlCommand(createAssignments, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                await EnsureSchoolColumnsAsync(connection);
                var classTenant = await TenantContext.HasSchoolIdColumnAsync(connection, CLASSES_TABLE);

                for (int i = 0; i < Common.AppConfig.ClassNames.Length; i++)
                {
                    string className = Common.AppConfig.ClassNames[i];
                    var checkSql = "SELECT COUNT(*) FROM Classes WHERE ClassName = ?";
                    if (classTenant) checkSql += TenantContext.FilterClauseSql();
                    using (var check = new SqlCommand(checkSql, connection))
                    {
                        check.AddPositionalParameter(className);
                        if (classTenant) TenantContext.AddSchoolParameter(check);
                        bool exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
                        if (exists)
                        {
                            continue;
                        }
                    }

                    var insertSql = classTenant
                        ? "INSERT INTO Classes (ClassName, TuitionFee, PromotionLevel, SchoolId) VALUES (?, ?, ?, ?)"
                        : "INSERT INTO Classes (ClassName, TuitionFee, PromotionLevel) VALUES (?, ?, ?)";
                    using (var insert = new SqlCommand(insertSql, connection))
                    {
                        insert.AddPositionalParameter(className);
                        insert.AddPositionalParameter(SchoolInfoRepository.LegacyFeeForClass(className));
                        insert.AddPositionalParameter(i + 1);
                        if (classTenant) TenantContext.AddSchoolParameter(insert);
                        await insert.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<DataTable> GetAllClassesTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, CLASSES_TABLE);
                    var query = $"SELECT ClassName, TuitionFee, PromotionLevel FROM {CLASSES_TABLE} WHERE 1=1";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    query += " ORDER BY PromotionLevel";
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        table.Columns.Add("ClassName", typeof(string));
                        table.Columns.Add("TuitionFee", typeof(decimal));
                        table.Columns.Add("PromotionLevel", typeof(int));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = table.NewRow();
                                row["ClassName"] = reader["ClassName"]?.ToString() ?? "";
                                row["TuitionFee"] = reader["TuitionFee"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TuitionFee"]);
                                row["PromotionLevel"] = reader["PromotionLevel"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PromotionLevel"]);
                                table.Rows.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving classes table", ex);
                throw new DataException("Error retrieving classes table", ex);
            }
            return table;
        }

        public async Task<bool> SaveClassAsync(KingdomPrep.Shared.Models.ClassConfig config, string originalClassName = null)
        {
            try
            {
                if (config == null || string.IsNullOrWhiteSpace(config.ClassName))
                {
                    return false;
                }

                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    string currentName = string.IsNullOrWhiteSpace(originalClassName)
                        ? config.ClassName
                        : originalClassName.Trim().ToUpperInvariant();

                    var checkQuery = $"SELECT COUNT(*) FROM {CLASSES_TABLE} WHERE ClassName = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, CLASSES_TABLE);
                    if (tenant) checkQuery += TenantContext.FilterClauseSql();
                    bool originalExists;
                    using (var checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.AddPositionalParameter(currentName);
                        if (tenant) TenantContext.AddSchoolParameter(checkCmd);
                        originalExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }

                    if (originalExists)
                    {
                        if (!string.Equals(currentName, config.ClassName, StringComparison.OrdinalIgnoreCase))
                        {
                            using (var duplicateCheck = new SqlCommand(checkQuery, connection))
                            {
                                duplicateCheck.AddPositionalParameter(config.ClassName);
                                if (tenant) TenantContext.AddSchoolParameter(duplicateCheck);
                                if (Convert.ToInt32(await duplicateCheck.ExecuteScalarAsync()) > 0)
                                {
                                    return false;
                                }
                            }
                        }

                        var updateSql = $"UPDATE {CLASSES_TABLE} SET ClassName = ?, TuitionFee = ?, PromotionLevel = ? WHERE ClassName = ?";
                        if (tenant) updateSql += TenantContext.FilterClauseSql();
                        using (var command = new SqlCommand(updateSql, connection))
                        {
                            command.AddPositionalParameter(config.ClassName);
                            command.AddPositionalParameter(config.TuitionFee);
                            command.AddPositionalParameter(config.PromotionLevel);
                            command.AddPositionalParameter(currentName);
                            if (tenant) TenantContext.AddSchoolParameter(command);
                            var result = await command.ExecuteNonQueryAsync();

                            if (result > 0 && !string.Equals(currentName, config.ClassName, StringComparison.OrdinalIgnoreCase))
                            {
                                await RenameClassReferencesAsync(connection, currentName, config.ClassName);
                            }

                            if (result > 0) await TryRecordClassUpsertAsync(config.ClassName, "Update");
                            return result > 0;
                        }
                    }

                    var insertSql = tenant
                        ? $"INSERT INTO {CLASSES_TABLE} (ClassName, TuitionFee, PromotionLevel, SchoolId) VALUES (?, ?, ?, ?)"
                        : $"INSERT INTO {CLASSES_TABLE} (ClassName, TuitionFee, PromotionLevel) VALUES (?, ?, ?)";
                    using (var command = new SqlCommand(insertSql, connection))
                    {
                        command.AddPositionalParameter(config.ClassName);
                        command.AddPositionalParameter(config.TuitionFee);
                        command.AddPositionalParameter(config.PromotionLevel);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0) await TryRecordClassUpsertAsync(config.ClassName, "Insert");
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error saving class configuration for {config?.ClassName}", ex);
                return false;
            }
        }

        private static async Task RenameClassReferencesAsync(SqlConnection connection, string oldClassName, string newClassName)
        {
            string[] tables = { "ClassAssignments", "ClassSubjects", "ClassFees" };
            foreach (string table in tables)
            {
                bool tenant = await TenantContext.HasSchoolIdColumnAsync(connection, table);
                var sql = $"IF OBJECT_ID(N'{table}', N'U') IS NOT NULL UPDATE {table} SET ClassName = ? WHERE ClassName = ?";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.AddPositionalParameter(newClassName);
                    command.AddPositionalParameter(oldClassName);
                    if (tenant) TenantContext.AddSchoolParameter(command);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteClassAsync(string className)
        {
            try
            {
                await TryRecordClassDeleteAsync(className);
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = $"DELETE FROM {CLASSES_TABLE} WHERE ClassName = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, CLASSES_TABLE);
                    if (tenant) query += TenantContext.FilterClauseSql();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(className);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error deleting class configuration for {className}", ex);
                return false;
            }
        }

        public async Task<KingdomPrep.Shared.Models.ClassConfig> GetByClassNameAsync(string className)
        {
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {CLASSES_TABLE} WHERE ClassName = ?";
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, CLASSES_TABLE);
                    if (tenant) query += TenantContext.FilterClauseSql();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.AddPositionalParameter(className);
                        if (tenant) TenantContext.AddSchoolParameter(command);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return new KingdomPrep.Shared.Models.ClassConfig
                                {
                                    ClassName = reader["ClassName"].ToString(),
                                    TuitionFee = Convert.ToDecimal(reader["TuitionFee"]),
                                    PromotionLevel = Convert.ToInt32(reader["PromotionLevel"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving class by name: {className}", ex);
            }
            return null;
        }

        public async Task<IEnumerable<string>> GetClassesForTeacherAsync(int employmentId)
        {
            var classes = new List<string>();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, ASSIGNMENTS_TABLE);
                    var query = "SELECT ClassName FROM ClassAssignments WHERE ClassTeacherID = ?";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    query += " ORDER BY ClassName";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.AddPositionalParameter(employmentId);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync()) classes.Add(reader["ClassName"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error retrieving classes for teacher {employmentId}", ex);
            }
            return classes;
        }

        public async Task<IEnumerable<(string ClassName, int? CurrentTeacherID)>> GetAllClassAssignmentsAsync()
        {
            var rows = new List<(string, int?)>();
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, ASSIGNMENTS_TABLE);
                    var query = "SELECT ClassName, ClassTeacherID FROM ClassAssignments WHERE 1=1";
                    if (tenant) query += TenantContext.FilterClauseSql();
                    query += " ORDER BY ClassName";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string name = reader["ClassName"].ToString();
                                int? teacher = reader["ClassTeacherID"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(reader["ClassTeacherID"]);
                                rows.Add((name, teacher));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving class assignments", ex);
            }
            return rows;
        }

        public async Task<IEnumerable<ClassAssignment>> GetAllDetailedAssignmentsAsync()
        {
            var list = new List<ClassAssignment>();
            try
            {
                using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await c.OpenAsync();
                    var assignmentTenant = await TenantContext.HasSchoolIdColumnAsync(c, ASSIGNMENTS_TABLE);
                    var employeeTenant = await TenantContext.HasSchoolIdColumnAsync(c, "Employee");
                    var sql = @"SELECT ca.ClassName, ca.ClassTeacherID, e.fullName as ClassTeacherName, ca.AssignedDate
                                         FROM ClassAssignments ca LEFT JOIN Employee e ON ca.ClassTeacherID = e.employmentID";
                    if (employeeTenant) sql += TenantContext.FilterClauseSql("e");
                    sql += " WHERE 1=1";
                    if (assignmentTenant) sql += TenantContext.FilterClauseSql("ca");
                    using (var cmd = new SqlCommand(sql, c))
                    {
                        if (employeeTenant) TenantContext.AddSchoolParameter(cmd);
                        if (assignmentTenant) TenantContext.AddSchoolParameter(cmd);
                        using (var r = await cmd.ExecuteReaderAsync())
                            while (await r.ReadAsync())
                                list.Add(new ClassAssignment {
                                    ClassName = r["ClassName"].ToString(),
                                    ClassTeacherID = r["ClassTeacherID"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ClassTeacherID"]),
                                    ClassTeacherName = r["ClassTeacherName"]?.ToString() ?? "Unassigned",
                                    AssignedDate = r["AssignedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["AssignedDate"])
                                });
                    }
                }
            }
            catch (Exception ex) { Services.LoggerHelper.LogWarning("GetAllDetailedAssignmentsAsync: " + ex.Message); }
            return list;
        }

        public async Task<bool> AssignTeacherToClassAsync(string className, int? employmentId)
        {
            try
            {
                using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await c.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(c, ASSIGNMENTS_TABLE);

                    // 1. Ensure row exists
                    var checkSql = "SELECT COUNT(*) FROM ClassAssignments WHERE ClassName = ?";
                    if (tenant) checkSql += TenantContext.FilterClauseSql();
                    using (var check = new SqlCommand(checkSql, c))
                    {
                        check.AddPositionalParameter(className);
                        if (tenant) TenantContext.AddSchoolParameter(check);
                        if (Convert.ToInt32(await check.ExecuteScalarAsync()) == 0)
                        {
                            var insertSql = tenant
                                ? "INSERT INTO ClassAssignments (ClassName, SchoolId) VALUES (?, ?)"
                                : "INSERT INTO ClassAssignments (ClassName) VALUES (?)";
                            using (var ins = new SqlCommand(insertSql, c))
                            {
                                ins.AddPositionalParameter(className);
                                if (tenant) TenantContext.AddSchoolParameter(ins);
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // 2. Update assignment
                    var sql = "UPDATE ClassAssignments SET ClassTeacherID = ?, AssignedDate = ? WHERE ClassName = ?";
                    if (tenant) sql += TenantContext.FilterClauseSql();
                    using (var cmd = new SqlCommand(sql, c))
                    {
                        cmd.AddPositionalParameter((object)employmentId ?? DBNull.Value);
                        cmd.AddPositionalParameter(DateTime.Today);
                        cmd.AddPositionalParameter(className);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        await cmd.ExecuteNonQueryAsync();
                        await TryRecordAssignmentUpsertAsync(className, "Update");
                        return true;
                    }
                }
            }
            catch (Exception ex) { Services.LoggerHelper.LogError("AssignTeacherToClassAsync failed", ex); return false; }
        }

        public async Task SetClassAssignmentsForTeacherAsync(int employmentId, IEnumerable<string> classNames)
        {
            var newSet = new HashSet<string>(classNames ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var oldSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();

                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, ASSIGNMENTS_TABLE);
                    var oldSql = "SELECT ClassName FROM ClassAssignments WHERE ClassTeacherID = ?";
                    if (tenant) oldSql += TenantContext.FilterClauseSql();
                    using (var old = new SqlCommand(oldSql, connection))
                    {
                        old.AddPositionalParameter(employmentId);
                        if (tenant) TenantContext.AddSchoolParameter(old);
                        using (var reader = await old.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync()) oldSet.Add(reader["ClassName"].ToString());
                        }
                    }

                    var unassignSql = "UPDATE ClassAssignments SET ClassTeacherID = NULL WHERE ClassTeacherID = ?";
                    if (tenant) unassignSql += TenantContext.FilterClauseSql();
                    using (var unassign = new SqlCommand(unassignSql, connection))
                    {
                        unassign.AddPositionalParameter(employmentId);
                        if (tenant) TenantContext.AddSchoolParameter(unassign);
                        await unassign.ExecuteNonQueryAsync();
                    }

                    foreach (string className in newSet)
                    {
                        await AssignTeacherToClassAsync(className, employmentId);
                    }

                    foreach (var className in oldSet.Union(newSet, StringComparer.OrdinalIgnoreCase))
                    {
                        await TryRecordAssignmentUpsertAsync(className, "Update");
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError($"Error setting class assignments for teacher {employmentId}", ex);
                throw;
            }
        }

        private static async Task EnsureSchoolColumnsAsync(SqlConnection connection)
        {
            var schoolId = TenantContext.CurrentSchoolId;
            if (schoolId == Guid.Empty) return;
            var school = schoolId.ToString("D");
            var sql = $@"
IF OBJECT_ID(N'Classes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Classes','SchoolId') IS NULL
        EXEC('ALTER TABLE [Classes] ADD SchoolId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Classes_SchoolId] DEFAULT (''{school}'')');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Classes_SchoolId' AND object_id = OBJECT_ID(N'Classes'))
        EXEC('CREATE INDEX [IX_Classes_SchoolId] ON [Classes](SchoolId)');
END
IF OBJECT_ID(N'ClassAssignments', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('ClassAssignments','SchoolId') IS NULL
        EXEC('ALTER TABLE [ClassAssignments] ADD SchoolId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ClassAssignments_SchoolId] DEFAULT (''{school}'')');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassAssignments_SchoolId' AND object_id = OBJECT_ID(N'ClassAssignments'))
        EXEC('CREATE INDEX [IX_ClassAssignments_SchoolId] ON [ClassAssignments](SchoolId)');
END";
            using (var command = new SqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task TryRecordClassUpsertAsync(string className, string operation)
        {
            await TryRecordSyncUpsertAsync(CLASSES_TABLE, "ClassName", className, operation, "Class sync capture skipped: ");
        }

        private async Task TryRecordAssignmentUpsertAsync(string className, string operation)
        {
            await TryRecordSyncUpsertAsync(ASSIGNMENTS_TABLE, "ClassName", className, operation, "Class assignment sync capture skipped: ");
        }

        private async Task TryRecordClassDeleteAsync(string className)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(className)) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(CLASSES_TABLE, "ClassName", className);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Class delete sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncUpsertAsync(string tableName, string primaryKeyName, string primaryKeyValue, string operation, string logPrefix)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(primaryKeyValue)) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(tableName, primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning(logPrefix + ex.Message);
            }
        }
    }
}

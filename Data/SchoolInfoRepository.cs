using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public sealed class SchoolIdentityHealth
    {
        public string SchoolName { get; set; } = "";
        public Guid SchoolId { get; set; }
        public DateTime? SetupCompletedAt { get; set; }
        public bool SetupAuditFound { get; set; }
        public int UserCount { get; set; }
        public int TenantIssueRows { get; set; }
    }

    /// <summary>
    /// Persists school identity (single row, Id = 1) and per-class term fees.
    /// Tables are created and seeded from the current hardcoded defaults on first use.
    /// SQL Server via Microsoft.Data.SqlClient.
    /// </summary>
    public class SchoolInfoRepository : ISchoolInfoRepository
    {
        private readonly string _connectionString;

        public SchoolInfoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();

                const string createInfo = @"IF OBJECT_ID(N'SchoolInformation', N'U') IS NULL
                    CREATE TABLE SchoolInformation (
                        Id INT NOT NULL PRIMARY KEY,
                        Name NVARCHAR(200), Address NVARCHAR(250), PoBox NVARCHAR(100),
                        GpsAddress NVARCHAR(50), Phone1 NVARCHAR(50), Phone2 NVARCHAR(50),
                        Email NVARCHAR(150), Logo VARBINARY(MAX), AdmissionFee MONEY,
                        UpdatedDate DATETIME);";
                using (var cmd = new SqlCommand(createInfo, c)) await cmd.ExecuteNonQueryAsync();

                // Add report-card colour columns to existing installs (idempotent).
                var def = new SchoolInformation();
                string[] colAlters =
                {
                    "IF COL_LENGTH('SchoolInformation','SchoolId') IS NULL ALTER TABLE SchoolInformation ADD SchoolId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SI_SchoolId DEFAULT NEWID()",
                    $"IF COL_LENGTH('SchoolInformation','PrimaryColor') IS NULL ALTER TABLE SchoolInformation ADD PrimaryColor INT NOT NULL CONSTRAINT DF_SI_Primary DEFAULT ({def.PrimaryColorArgb})",
                    $"IF COL_LENGTH('SchoolInformation','AccentColor') IS NULL ALTER TABLE SchoolInformation ADD AccentColor INT NOT NULL CONSTRAINT DF_SI_Accent DEFAULT ({def.AccentColorArgb})",
                    $"IF COL_LENGTH('SchoolInformation','SecondaryColor') IS NULL ALTER TABLE SchoolInformation ADD SecondaryColor INT NOT NULL CONSTRAINT DF_SI_Secondary DEFAULT ({def.SecondaryColorArgb})",
                    "IF COL_LENGTH('SchoolInformation','PortalUrl') IS NULL ALTER TABLE SchoolInformation ADD PortalUrl NVARCHAR(200) NOT NULL CONSTRAINT DF_SI_PortalUrl DEFAULT ('')"
                };
                foreach (var alter in colAlters)
                    using (var cmd = new SqlCommand(alter, c)) await cmd.ExecuteNonQueryAsync();

                const string createFees = @"IF OBJECT_ID(N'ClassFees', N'U') IS NULL
                    CREATE TABLE ClassFees (
                        ClassName NVARCHAR(50) NOT NULL PRIMARY KEY,
                        TermFee MONEY);";
                using (var cmd = new SqlCommand(createFees, c)) await cmd.ExecuteNonQueryAsync();

                // Seed identity row if absent.
                bool hasInfo;
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM SchoolInformation WHERE Id = 1", c))
                    hasInfo = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                if (!hasInfo)
                {
                    var d = new SchoolInformation();
                    const string ins = @"INSERT INTO SchoolInformation
                        (Id,SchoolId,Name,Address,PoBox,GpsAddress,Phone1,Phone2,Email,Logo,AdmissionFee,UpdatedDate)
                        VALUES (1,?,?,?,?,?,?,?,?,?,?,?)";
                    using (var cmd = new SqlCommand(ins, c))
                    {
                        cmd.AddPositionalParameter(d.SchoolId);
                        cmd.AddPositionalParameter(d.Name);
                        cmd.AddPositionalParameter(d.Address);
                        cmd.AddPositionalParameter(d.PoBox);
                        cmd.AddPositionalParameter(d.GpsAddress);
                        cmd.AddPositionalParameter(d.Phone1);
                        cmd.AddPositionalParameter(d.Phone2);
                        cmd.AddPositionalParameter(d.Email);
                        cmd.AddPositionalParameter(DBNull.Value, SqlDbType.VarBinary);
                        cmd.AddPositionalParameter(d.AdmissionFee);
                        cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Seed per-class fees for any class not yet present.
                foreach (var className in AppConfig.ClassNames)
                {
                    bool exists;
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM ClassFees WHERE ClassName = ?", c))
                    {
                        cmd.AddPositionalParameter(className);
                        exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (!exists)
                    {
                        using (var cmd = new SqlCommand("INSERT INTO ClassFees (ClassName, TermFee) VALUES (?, ?)", c))
                        {
                            cmd.AddPositionalParameter(className);
                            cmd.AddPositionalParameter(LegacyFeeForClass(className));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        public async Task<SchoolInformation> GetAsync()
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand("SELECT * FROM SchoolInformation WHERE Id = 1", c))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync()) return new SchoolInformation(); // defaults
                    return new SchoolInformation
                    {
                        SchoolId = AsGuid(r, "SchoolId"),
                        Name = AsString(r["Name"]),
                        Address = AsString(r["Address"]),
                        PoBox = AsString(r["PoBox"]),
                        GpsAddress = AsString(r["GpsAddress"]),
                        Phone1 = AsString(r["Phone1"]),
                        Phone2 = AsString(r["Phone2"]),
                        Email = AsString(r["Email"]),
                        PortalUrl = AsString(r["PortalUrl"]),
                        Logo = r["Logo"] as byte[],
                        AdmissionFee = Convert.ToDecimal(r["AdmissionFee"]),
                        PrimaryColorArgb = AsInt(r, "PrimaryColor", new SchoolInformation().PrimaryColorArgb),
                        AccentColorArgb = AsInt(r, "AccentColor", new SchoolInformation().AccentColorArgb),
                        SecondaryColorArgb = AsInt(r, "SecondaryColor", new SchoolInformation().SecondaryColorArgb),
                        UpdatedDate = Convert.ToDateTime(r["UpdatedDate"])
                    };
                }
            }
        }

        public async Task<SchoolIdentityHealth> GetIdentityHealthAsync()
        {
            await EnsureTablesAsync();
            var info = await GetAsync();
            var health = new SchoolIdentityHealth
            {
                SchoolName = info.Name ?? "",
                SchoolId = info.SchoolId
            };

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();

                if (await TableExistsAsync(c, "Users"))
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Users", c))
                        health.UserCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    if (await ColumnExistsAsync(c, "Users", "SchoolId") && health.SchoolId != Guid.Empty)
                    {
                        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE SchoolId IS NULL OR SchoolId <> ?", c))
                        {
                            cmd.AddPositionalParameter(health.SchoolId);
                            health.TenantIssueRows += Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }
                    }
                }

                await EnsureSetupAuditTableAsync(c);
                using (var cmd = new SqlCommand("SELECT TOP 1 CompletedAt FROM SystemSetupAudit WHERE EventType = 'FirstRunCompleted' ORDER BY CompletedAt DESC", c))
                {
                    var raw = await cmd.ExecuteScalarAsync();
                    if (raw != null && raw != DBNull.Value)
                    {
                        health.SetupAuditFound = true;
                        health.SetupCompletedAt = Convert.ToDateTime(raw);
                    }
                }
            }

            return health;
        }

        public async Task RepairTenantDataAsync()
        {
            await EnsureTablesAsync();
            var info = await GetAsync();
            if (info.SchoolId == Guid.Empty) return;

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                foreach (var table in new[] { "Users", "Students", "Employee", "payment_record", "fees", "examss", "Attendance" })
                {
                    if (!await TableExistsAsync(c, table) || !await ColumnExistsAsync(c, table, "SchoolId")) continue;
                    using (var cmd = new SqlCommand($"UPDATE [{table}] SET SchoolId = ? WHERE SchoolId IS NULL", c))
                    {
                        cmd.AddPositionalParameter(info.SchoolId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task RecordSystemRecoveryAsync(string username, string notes)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await EnsureSetupAuditTableAsync(c);
                using (var cmd = new SqlCommand(@"INSERT INTO SystemSetupAudit (EventType, Username, Notes, CompletedAt)
VALUES ('SystemRecovery', ?, ?, GETDATE())", c))
                {
                    cmd.AddPositionalParameter(username ?? "");
                    cmd.AddPositionalParameter(notes ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task RecordFirstRunCompletedAsync(string username)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await EnsureSetupAuditTableAsync(c);
                using (var cmd = new SqlCommand(@"INSERT INTO SystemSetupAudit (EventType, Username, Notes, CompletedAt)
VALUES ('FirstRunCompleted', ?, '', GETDATE())", c))
                {
                    cmd.AddPositionalParameter(username ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Dictionary<string, decimal>> GetClassFeesAsync()
        {
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand("SELECT ClassName, TermFee FROM ClassFees", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        map[AsString(r["ClassName"])] = Convert.ToDecimal(r["TermFee"]);
            }
            return map;
        }

        public async Task<List<string>> GetClassNamesAsync()
        {
            var list = new List<string>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                using (var cmd = new SqlCommand("SELECT ClassName FROM ClassFees ORDER BY ClassName", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(AsString(r["ClassName"]));
            }
            return list;
        }

        public async Task SaveAsync(SchoolInformation info)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE SchoolInformation SET
                    SchoolId=?, Name=?, Address=?, PoBox=?, GpsAddress=?, Phone1=?, Phone2=?, Email=?, PortalUrl=?,
                    Logo=?, AdmissionFee=?, PrimaryColor=?, AccentColor=?, SecondaryColor=?, UpdatedDate=? WHERE Id = 1";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(info.SchoolId == Guid.Empty ? Guid.NewGuid() : info.SchoolId);
                    cmd.AddPositionalParameter(info.Name ?? "");
                    cmd.AddPositionalParameter(info.Address ?? "");
                    cmd.AddPositionalParameter(info.PoBox ?? "");
                    cmd.AddPositionalParameter(info.GpsAddress ?? "");
                    cmd.AddPositionalParameter(info.Phone1 ?? "");
                    cmd.AddPositionalParameter(info.Phone2 ?? "");
                    cmd.AddPositionalParameter(info.Email ?? "");
                    cmd.AddPositionalParameter(info.PortalUrl ?? "");
                    cmd.AddPositionalParameter((object)info.Logo ?? DBNull.Value, SqlDbType.VarBinary);
                    cmd.AddPositionalParameter(info.AdmissionFee);
                    cmd.AddPositionalParameter(info.PrimaryColorArgb);
                    cmd.AddPositionalParameter(info.AccentColorArgb);
                    cmd.AddPositionalParameter(info.SecondaryColorArgb);
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0) // row missing somehow — ensure then retry once
                    {
                        await EnsureTablesAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                    await TryRecordSyncUpsertAsync("SchoolInformation", "Id", 1, "Update");
                }
            }
        }

        public async Task SaveClassFeesAsync(IDictionary<string, decimal> fees)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();

                var existingClasses = new List<string>();
                using (var cmd = new SqlCommand("SELECT ClassName FROM ClassFees", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) existingClasses.Add(AsString(r["ClassName"]));

                foreach (var e in existingClasses)
                {
                    if (!fees.ContainsKey(e))
                    {
                        using (var del = new SqlCommand("DELETE FROM ClassFees WHERE ClassName = ?", c))
                        {
                            await TryRecordSyncDeleteAsync("ClassFees", "ClassName", e);
                            del.AddPositionalParameter(e);
                            await del.ExecuteNonQueryAsync();
                        }
                    }
                }

                foreach (var kv in fees)
                {
                    using (var upd = new SqlCommand("UPDATE ClassFees SET TermFee = ? WHERE ClassName = ?", c))
                    {
                        upd.AddPositionalParameter(kv.Value);
                        upd.AddPositionalParameter(kv.Key);
                        if (await upd.ExecuteNonQueryAsync() == 0)
                        {
                            using (var ins = new SqlCommand("INSERT INTO ClassFees (ClassName, TermFee) VALUES (?, ?)", c))
                            {
                                ins.AddPositionalParameter(kv.Key);
                                ins.AddPositionalParameter(kv.Value);
                                await ins.ExecuteNonQueryAsync();
                            }
                            await TryRecordSyncUpsertAsync("ClassFees", "ClassName", kv.Key, "Insert");
                        }
                        else
                        {
                            await TryRecordSyncUpsertAsync("ClassFees", "ClassName", kv.Key, "Update");
                        }
                    }
                }
            }
        }

        // SQL 'datetime' rejects sub-second precision from SQL Server; drop it.
        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

        private static string AsString(object o) => o == null || o == DBNull.Value ? "" : o.ToString();

        private static Guid AsGuid(System.Data.IDataRecord r, string col)
        {
            try
            {
                var value = r[col];
                if (value == null || value == DBNull.Value) return Guid.NewGuid();
                if (value is Guid id) return id;
                return Guid.TryParse(value.ToString(), out var parsed) ? parsed : Guid.NewGuid();
            }
            catch
            {
                return Guid.NewGuid();
            }
        }

        private static int AsInt(System.Data.IDataRecord r, string col, int fallback)
        {
            try { var v = r[col]; return v == null || v == DBNull.Value ? fallback : Convert.ToInt32(v); }
            catch { return fallback; }
        }

        private static async Task<bool> TableExistsAsync(SqlConnection c, string tableName)
        {
            using (var cmd = new SqlCommand("SELECT OBJECT_ID(?)", c))
            {
                cmd.AddPositionalParameter(tableName);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection c, string tableName, string columnName)
        {
            using (var cmd = new SqlCommand($"SELECT COL_LENGTH('{tableName.Replace("'", "''")}', ?)", c))
            {
                cmd.AddPositionalParameter(columnName);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        private static async Task EnsureSetupAuditTableAsync(SqlConnection c)
        {
            using (var cmd = new SqlCommand(@"IF OBJECT_ID(N'SystemSetupAudit', N'U') IS NULL
CREATE TABLE SystemSetupAudit (
    AuditID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EventType NVARCHAR(60) NOT NULL,
    Username NVARCHAR(120) NULL,
    Notes NVARCHAR(500) NULL,
    CompletedAt DATETIME NOT NULL
);", c))
            {
                await cmd.ExecuteNonQueryAsync();
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
                Services.LoggerHelper.LogWarning("School settings sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(string tableName, string primaryKeyName, object primaryKeyValue)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(tableName, primaryKeyName, primaryKeyValue);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("School settings delete sync capture skipped: " + ex.Message);
            }
        }

        // Mirrors the legacy StudentService.GetFeeForClass switch so the seed matches today.
        public static decimal LegacyFeeForClass(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId)) return 1200m;
            switch (classId.Trim().ToUpperInvariant())
            {
                case "CRECHE": return 2000m;
                case "NURSERY 1": return 3450m;
                case "NURSERY 2": return 3750m;
                case "KINDERGARTEN 1": return 3654m;
                case "KINDERGARTEN 2":
                case "BASIC 1":
                case "BASIC 2":
                case "BASIC 3":
                case "BASIC 4":
                case "BASIC 5":
                case "BASIC 6":
                case "BASIC 7":
                case "BASIC 8":
                case "BASIC 9": return 2423m;
                default: return 1200m;
            }
        }
    }
}

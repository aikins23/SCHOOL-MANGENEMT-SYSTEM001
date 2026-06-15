using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Persists school identity (single row, Id = 1) and per-class term fees.
    /// Tables are created and seeded from the current hardcoded defaults on first use.
    /// SQL Server (LocalDB) via OleDb.
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
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();

                const string createInfo = @"IF OBJECT_ID(N'SchoolInformation', N'U') IS NULL
                    CREATE TABLE SchoolInformation (
                        Id INT NOT NULL PRIMARY KEY,
                        Name NVARCHAR(200), Address NVARCHAR(250), PoBox NVARCHAR(100),
                        GpsAddress NVARCHAR(50), Phone1 NVARCHAR(50), Phone2 NVARCHAR(50),
                        Email NVARCHAR(150), Logo VARBINARY(MAX), AdmissionFee MONEY,
                        UpdatedDate DATETIME);";
                using (var cmd = new OleDbCommand(createInfo, c)) await cmd.ExecuteNonQueryAsync();

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
                    using (var cmd = new OleDbCommand(alter, c)) await cmd.ExecuteNonQueryAsync();

                const string createFees = @"IF OBJECT_ID(N'ClassFees', N'U') IS NULL
                    CREATE TABLE ClassFees (
                        ClassName NVARCHAR(50) NOT NULL PRIMARY KEY,
                        TermFee MONEY);";
                using (var cmd = new OleDbCommand(createFees, c)) await cmd.ExecuteNonQueryAsync();

                // Seed identity row if absent.
                bool hasInfo;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM SchoolInformation WHERE Id = 1", c))
                    hasInfo = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                if (!hasInfo)
                {
                    var d = new SchoolInformation();
                    const string ins = @"INSERT INTO SchoolInformation
                        (Id,SchoolId,Name,Address,PoBox,GpsAddress,Phone1,Phone2,Email,Logo,AdmissionFee,UpdatedDate)
                        VALUES (1,?,?,?,?,?,?,?,?,?,?,?)";
                    using (var cmd = new OleDbCommand(ins, c))
                    {
                        cmd.Parameters.AddWithValue("?", d.SchoolId);
                        cmd.Parameters.AddWithValue("?", d.Name);
                        cmd.Parameters.AddWithValue("?", d.Address);
                        cmd.Parameters.AddWithValue("?", d.PoBox);
                        cmd.Parameters.AddWithValue("?", d.GpsAddress);
                        cmd.Parameters.AddWithValue("?", d.Phone1);
                        cmd.Parameters.AddWithValue("?", d.Phone2);
                        cmd.Parameters.AddWithValue("?", d.Email);
                        cmd.Parameters.Add("?", OleDbType.VarBinary).Value = DBNull.Value;
                        cmd.Parameters.AddWithValue("?", d.AdmissionFee);
                        cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Seed per-class fees for any class not yet present.
                foreach (var className in AppConfig.ClassNames)
                {
                    bool exists;
                    using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM ClassFees WHERE ClassName = ?", c))
                    {
                        cmd.Parameters.AddWithValue("?", className);
                        exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (!exists)
                    {
                        using (var cmd = new OleDbCommand("INSERT INTO ClassFees (ClassName, TermFee) VALUES (?, ?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", className);
                            cmd.Parameters.AddWithValue("?", LegacyFeeForClass(className));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        public async Task<SchoolInformation> GetAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT * FROM SchoolInformation WHERE Id = 1", c))
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

        public async Task<Dictionary<string, decimal>> GetClassFeesAsync()
        {
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT ClassName, TermFee FROM ClassFees", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        map[AsString(r["ClassName"])] = Convert.ToDecimal(r["TermFee"]);
            }
            return map;
        }

        public async Task SaveAsync(SchoolInformation info)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE SchoolInformation SET
                    SchoolId=?, Name=?, Address=?, PoBox=?, GpsAddress=?, Phone1=?, Phone2=?, Email=?, PortalUrl=?,
                    Logo=?, AdmissionFee=?, PrimaryColor=?, AccentColor=?, SecondaryColor=?, UpdatedDate=? WHERE Id = 1";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", info.SchoolId == Guid.Empty ? Guid.NewGuid() : info.SchoolId);
                    cmd.Parameters.AddWithValue("?", info.Name ?? "");
                    cmd.Parameters.AddWithValue("?", info.Address ?? "");
                    cmd.Parameters.AddWithValue("?", info.PoBox ?? "");
                    cmd.Parameters.AddWithValue("?", info.GpsAddress ?? "");
                    cmd.Parameters.AddWithValue("?", info.Phone1 ?? "");
                    cmd.Parameters.AddWithValue("?", info.Phone2 ?? "");
                    cmd.Parameters.AddWithValue("?", info.Email ?? "");
                    cmd.Parameters.AddWithValue("?", info.PortalUrl ?? "");
                    cmd.Parameters.Add("?", OleDbType.VarBinary).Value =
                        (object)info.Logo ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("?", info.AdmissionFee);
                    cmd.Parameters.AddWithValue("?", info.PrimaryColorArgb);
                    cmd.Parameters.AddWithValue("?", info.AccentColorArgb);
                    cmd.Parameters.AddWithValue("?", info.SecondaryColorArgb);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0) // row missing somehow — ensure then retry once
                    {
                        await EnsureTablesAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task SaveClassFeesAsync(IDictionary<string, decimal> fees)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                foreach (var kv in fees)
                {
                    using (var upd = new OleDbCommand("UPDATE ClassFees SET TermFee = ? WHERE ClassName = ?", c))
                    {
                        upd.Parameters.AddWithValue("?", kv.Value);
                        upd.Parameters.AddWithValue("?", kv.Key);
                        if (await upd.ExecuteNonQueryAsync() == 0)
                        {
                            using (var ins = new OleDbCommand("INSERT INTO ClassFees (ClassName, TermFee) VALUES (?, ?)", c))
                            {
                                ins.Parameters.AddWithValue("?", kv.Key);
                                ins.Parameters.AddWithValue("?", kv.Value);
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
        }

        // SQL 'datetime' rejects sub-second precision from MSOLEDBSQL; drop it.
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

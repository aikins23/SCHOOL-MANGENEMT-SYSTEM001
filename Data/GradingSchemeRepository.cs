using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Persists the school-wide grading scheme (ordered grade bands). Created and seeded
    /// from the legacy 5-band scheme on first use. SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class GradingSchemeRepository : IGradingSchemeRepository
    {
        private readonly string _connectionString;

        public GradingSchemeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string create = @"IF OBJECT_ID(N'GradingScheme', N'U') IS NULL
                    CREATE TABLE GradingScheme (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        MinScore INT NOT NULL,
                        Code NVARCHAR(10),
                        Label NVARCHAR(80));";
                using (var cmd = new OleDbCommand(create, c)) await cmd.ExecuteNonQueryAsync();

                bool hasRows;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM GradingScheme", c))
                    hasRows = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                if (!hasRows)
                {
                    foreach (var b in LegacyBands())
                    {
                        using (var cmd = new OleDbCommand(
                            "INSERT INTO GradingScheme (MinScore, Code, Label) VALUES (?, ?, ?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", b.MinScore);
                            cmd.Parameters.AddWithValue("?", b.Code);
                            cmd.Parameters.AddWithValue("?", b.Label);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        /// <summary>EnsureTableAsync but swallows errors (used by the fail-safe accessor).</summary>
        public void EnsureTablesAsyncSafe()
        {
            try { EnsureTableAsync().GetAwaiter().GetResult(); } catch { /* accessor falls back */ }
        }

        public async Task<List<GradeBand>> GetBandsAsync()
        {
            var list = new List<GradeBand>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT MinScore, Code, Label FROM GradingScheme ORDER BY MinScore DESC", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new GradeBand
                        {
                            MinScore = Convert.ToInt32(r["MinScore"]),
                            Code = AsString(r["Code"]),
                            Label = AsString(r["Label"])
                        });
            }
            return list;
        }

        public async Task SaveBandsAsync(IEnumerable<GradeBand> bands)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var del = new OleDbCommand("DELETE FROM GradingScheme", c))
                    await del.ExecuteNonQueryAsync();
                foreach (var b in bands)
                {
                    using (var cmd = new OleDbCommand(
                        "INSERT INTO GradingScheme (MinScore, Code, Label) VALUES (?, ?, ?)", c))
                    {
                        cmd.Parameters.AddWithValue("?", b.MinScore);
                        cmd.Parameters.AddWithValue("?", b.Code ?? "");
                        cmd.Parameters.AddWithValue("?", b.Label ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>The hardcoded scheme used to seed the table and as the fail-safe default.</summary>
        public static List<GradeBand> LegacyBands() => new List<GradeBand>
        {
            new GradeBand { MinScore = 80, Code = "1", Label = "Advance(A)" },
            new GradeBand { MinScore = 75, Code = "2", Label = "Proficiency(P)" },
            new GradeBand { MinScore = 70, Code = "3", Label = "Approaching Proficiency(AP)" },
            new GradeBand { MinScore = 65, Code = "4", Label = "Developing" },
            new GradeBand { MinScore = 0,  Code = "5", Label = "Beginning" }
        };

        private static string AsString(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
    }
}

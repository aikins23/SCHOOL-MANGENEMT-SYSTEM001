using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// CRUD over the existing Expenses table + a configurable ExpenseCategories list. The legacy
    /// Amount column is varchar, so amounts are written comma-free (ExpenseAmount.Store) and read
    /// via ExpenseAmount.Parse — keeping the dashboard's TRY_CAST charts working. OleDb/MSOLEDBSQL.
    /// </summary>
    public class ExpenseRepository
    {
        private readonly string _connectionString;

        public ExpenseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string create = @"IF OBJECT_ID(N'ExpenseCategories', N'U') IS NULL
                    CREATE TABLE ExpenseCategories (Name NVARCHAR(60) NOT NULL PRIMARY KEY);";
                using (var cmd = new OleDbCommand(create, c)) await cmd.ExecuteNonQueryAsync();

                int count;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM ExpenseCategories", c))
                    count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (count == 0)
                {
                    foreach (var seed in new[] { "Salaries", "Utilities", "Supplies", "Maintenance", "Transport", "Rent", "Repairs", "Miscellaneous" })
                        using (var cmd = new OleDbCommand("INSERT INTO ExpenseCategories (Name) VALUES (?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", seed);
                            await cmd.ExecuteNonQueryAsync();
                        }
                }
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            var list = new List<string>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT Name FROM ExpenseCategories ORDER BY Name", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(S(r["Name"]));
            }
            return list;
        }

        public async Task AddCategoryAsync(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return;
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"IF NOT EXISTS (SELECT 1 FROM ExpenseCategories WHERE Name = ?)
                    INSERT INTO ExpenseCategories (Name) VALUES (?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", name);
                    cmd.Parameters.AddWithValue("?", name);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Expense>> GetByRangeAsync(DateTime from, DateTime to, string category = null)
        {
            var list = new List<Expense>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                string sql = @"SELECT ID, Expenses_name, Purpose, Description, Date_Time, Amount, Payee, payer
                    FROM Expenses WHERE Date_Time BETWEEN ? AND ?";
                bool byCat = !string.IsNullOrWhiteSpace(category);
                if (byCat) sql += " AND Purpose = ?";
                sql += " ORDER BY Date_Time DESC, ID DESC";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", from.Date);
                    cmd.Parameters.AddWithValue("?", to.Date);
                    if (byCat) cmd.Parameters.AddWithValue("?", category.Trim());
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync())
                            list.Add(new Expense
                            {
                                Id = I(r["ID"]),
                                Name = S(r["Expenses_name"]),
                                Category = S(r["Purpose"]),
                                Description = S(r["Description"]),
                                Date = r["Date_Time"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["Date_Time"]),
                                Amount = ExpenseAmount.Parse(S(r["Amount"])),
                                Payee = S(r["Payee"]),
                                Payer = S(r["payer"])
                            });
                }
            }
            return list;
        }

        public async Task<bool> AddAsync(Expense e)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO Expenses (Expenses_name, Purpose, Description, Date_Time, Amount, Payee, payer)
                    VALUES (?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", Trim(e.Name, 50));
                    cmd.Parameters.AddWithValue("?", Trim(e.Category, 50));
                    cmd.Parameters.AddWithValue("?", Trim(e.Description, 100));
                    cmd.Parameters.AddWithValue("?", e.Date.Date);
                    cmd.Parameters.AddWithValue("?", ExpenseAmount.Store(e.Amount));
                    cmd.Parameters.AddWithValue("?", Trim(e.Payee, 30));
                    cmd.Parameters.AddWithValue("?", Trim(e.Payer, 30));
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> UpdateAsync(Expense e)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE Expenses SET Expenses_name=?, Purpose=?, Description=?, Date_Time=?, Amount=?, Payee=?, payer=?
                    WHERE ID=?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", Trim(e.Name, 50));
                    cmd.Parameters.AddWithValue("?", Trim(e.Category, 50));
                    cmd.Parameters.AddWithValue("?", Trim(e.Description, 100));
                    cmd.Parameters.AddWithValue("?", e.Date.Date);
                    cmd.Parameters.AddWithValue("?", ExpenseAmount.Store(e.Amount));
                    cmd.Parameters.AddWithValue("?", Trim(e.Payee, 30));
                    cmd.Parameters.AddWithValue("?", Trim(e.Payer, 30));
                    cmd.Parameters.AddWithValue("?", e.Id);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("DELETE FROM Expenses WHERE ID=?", c))
                {
                    cmd.Parameters.AddWithValue("?", id);
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<decimal> GetTotalExpensesBetweenAsync(DateTime from, DateTime to)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT ISNULL(SUM(TRY_CAST(REPLACE(ISNULL(Amount,'0'),',','') AS DECIMAL(18,2))),0)
                    FROM Expenses WHERE Date_Time BETWEEN ? AND ?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", from.Date);
                    cmd.Parameters.AddWithValue("?", to.Date);
                    var o = await cmd.ExecuteScalarAsync();
                    return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                }
            }
        }

        private static string Trim(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) : s);
        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
        private static int I(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
    }
}

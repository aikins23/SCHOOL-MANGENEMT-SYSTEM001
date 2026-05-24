using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class ClassRepository : IClassRepository
    {
        private readonly string _connectionString;
        private const string CLASSES_TABLE = "Classes";

        public ClassRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task EnsureTableExistsAsync()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var checkQuery = "SELECT 1 FROM sys.tables WHERE name = ?";
                    using (var checkCmd = new OleDbCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("?", CLASSES_TABLE);
                        var exists = await checkCmd.ExecuteScalarAsync();
                        if (exists == null)
                        {
                            var createSql = $@"
                                CREATE TABLE {CLASSES_TABLE} (
                                    ClassName varchar(50) NOT NULL PRIMARY KEY,
                                    TuitionFee money NOT NULL DEFAULT 0,
                                    PromotionLevel int NOT NULL DEFAULT 1
                                )";
                            using (var createCmd = new OleDbCommand(createSql, connection))
                            {
                                await createCmd.ExecuteNonQueryAsync();
                            }
                            
                            // Seed initial data
                            var seedSql = $@"
                                INSERT INTO {CLASSES_TABLE} (ClassName, TuitionFee, PromotionLevel) VALUES ('CRECHE', 2000, 1);
                                INSERT INTO {CLASSES_TABLE} (ClassName, TuitionFee, PromotionLevel) VALUES ('NURSERY 1', 3450, 2);
                                INSERT INTO {CLASSES_TABLE} (ClassName, TuitionFee, PromotionLevel) VALUES ('BASIC 1', 2423, 5);";
                            using (var seedCmd = new OleDbCommand(seedSql, connection))
                            {
                                await seedCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public async Task<DataTable> GetAllClassesTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT ClassName, TuitionFee, PromotionLevel FROM {CLASSES_TABLE} ORDER BY PromotionLevel";
                    using (var command = new OleDbCommand(query, connection))
                    using (var adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error retrieving classes table", ex);
            }
            return table;
        }

        public async Task<bool> SaveClassAsync(Models.ClassConfig config)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var checkQuery = $"SELECT COUNT(*) FROM {CLASSES_TABLE} WHERE ClassName = ?";
                    bool exists;
                    using (var checkCmd = new OleDbCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("?", config.ClassName);
                        exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    }

                    string query;
                    if (exists)
                    {
                        query = $"UPDATE {CLASSES_TABLE} SET TuitionFee = ?, PromotionLevel = ? WHERE ClassName = ?";
                    }
                    else
                    {
                        query = $"INSERT INTO {CLASSES_TABLE} (TuitionFee, PromotionLevel, ClassName) VALUES (?, ?, ?)";
                    }

                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", config.TuitionFee);
                        command.Parameters.AddWithValue("?", config.PromotionLevel);
                        command.Parameters.AddWithValue("?", config.ClassName);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error saving class configuration", ex);
            }
        }

        public async Task<bool> DeleteClassAsync(string className)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"DELETE FROM {CLASSES_TABLE} WHERE ClassName = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", className);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error deleting class configuration", ex);
            }
        }

        public async Task<Models.ClassConfig> GetByClassNameAsync(string className)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {CLASSES_TABLE} WHERE ClassName = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", className);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return new Models.ClassConfig
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
            catch { }
            return null;
        }
    }
}

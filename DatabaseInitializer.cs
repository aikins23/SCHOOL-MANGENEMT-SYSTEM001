using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Database initialization utility to execute SQL schema scripts
    /// This utility creates database tables and indexes as defined in DatabaseOptimization.sql
    /// </summary>
    public static class DatabaseInitializer
    {
        // Connection string for SQL Server LocalDB
        private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=Neat_Academy;";

        public static async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DatabaseOptimization.sql");

                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"Database script not found at: {scriptPath}");
                    return false;
                }

                string scriptContent = File.ReadAllText(scriptPath);
                return await ExecuteScriptAsync(scriptContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ExecuteScriptAsync(string script)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to database successfully.");

                    // Split the script by GO statements (batch separator)
                    string[] batches = script.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO\n", "\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch))
                            continue;

                        using (SqlCommand command = new SqlCommand(batch, connection))
                        {
                            command.CommandTimeout = 300; // 5 minute timeout
                            await command.ExecuteNonQueryAsync();
                            Console.WriteLine("Executed batch successfully.");
                        }
                    }

                    Console.WriteLine("Database initialization completed successfully!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing database script: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Verify that the StudentTermRemarks table exists and has the correct schema
        /// </summary>
        public static async Task<bool> VerifyStudentTermRemarksTableAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = 'StudentTermRemarks' AND TABLE_SCHEMA = 'dbo'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        int tableCount = (int)await command.ExecuteScalarAsync();

                        if (tableCount > 0)
                        {
                            Console.WriteLine("StudentTermRemarks table verified to exist.");

                            // Verify columns
                            string columnQuery = @"
                                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                                WHERE TABLE_NAME = 'StudentTermRemarks' AND TABLE_SCHEMA = 'dbo'";

                            using (SqlCommand columnCommand = new SqlCommand(columnQuery, connection))
                            {
                                int columnCount = (int)await columnCommand.ExecuteScalarAsync();
                                Console.WriteLine($"StudentTermRemarks table has {columnCount} columns.");
                                return columnCount == 11; // Expected 11 columns
                            }
                        }
                        else
                        {
                            Console.WriteLine("StudentTermRemarks table not found!");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying table: {ex.Message}");
                return false;
            }
        }
    }
}

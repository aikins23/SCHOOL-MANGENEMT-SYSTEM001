using System;
using System.IO;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for database backup and recovery operations
    /// </summary>
    public static class DatabaseBackupService
    {
        public static async Task<(bool Success, string Message)> CreateBackupAsync()
        {
            try
            {
                // In a production SQL Server environment, we would use "BACKUP DATABASE" command
                // For LocalDB / MDF file deployments, we can copy the file if it's not locked.
                
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmartSchoolDB.mdf");
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmartSchoolDB_log.ldf");
                
                string backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KPS_Backups");
                if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupDbPath = Path.Combine(backupFolder, $"KPS_Backup_{timestamp}.mdf");
                string backupLogPath = Path.Combine(backupFolder, $"KPS_Backup_{timestamp}_log.ldf");

                await Task.Run(() => {
                    // Note: This might fail if SQL LocalDB has the file exclusively locked.
                    // A better approach is usually running a T-SQL BACKUP command via connection.
                    File.Copy(dbPath, backupDbPath, true);
                    if (File.Exists(logPath)) File.Copy(logPath, backupLogPath, true);
                });

                return (true, $"Backup created successfully at:\n{backupDbPath}");
            }
            catch (Exception ex)
            {
                return (false, "Backup failed: " + ex.Message);
            }
        }

        public static async Task<(bool Success, string Message)> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath)) return (false, "Backup file not found.");

                // Restore requires closing all connections and replacing the active MDF.
                // This is risky to do while the app is running.
                
                return (false, "Automatic restore is disabled while the application is active. Please contact support for manual restoration.");
            }
            catch (Exception ex)
            {
                return (false, "Restore failed: " + ex.Message);
            }
        }
    }
}

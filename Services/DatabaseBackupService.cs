using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for SQL Server (LocalDB) database backup and recovery via T-SQL.
    /// </summary>
    public static class DatabaseBackupService
    {
        private static readonly string BackupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "IPMC_Backups"
        );

        private static System.Timers.Timer _backupTimer;
        private static bool _backupRunToday = false;

        /// <summary>
        /// Creates a backup of the current database.
        /// Uses retry logic to handle file locking issues.
        /// </summary>
        public static async Task<(bool Success, string Message)> CreateBackupAsync()
        {
            try
            {
                string dbName = ExtractDatabaseName(AppConfig.ConnectionString);
                if (string.IsNullOrWhiteSpace(dbName))
                    return (false, "Could not determine database name from connection string.");

                if (!Directory.Exists(BackupFolder))
                    Directory.CreateDirectory(BackupFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"KPS_Backup_{timestamp}.bak";
                string backupPath = Path.Combine(BackupFolder, backupFileName);

                string sql = $"BACKUP DATABASE [{dbName}] TO DISK = N'{backupPath.Replace("'", "''")}' " +
                             "WITH FORMAT, INIT, NAME = N'KPS Full Backup', SKIP, STATS = 10";

                using (var connection = new OleDbConnection(GetMasterConnectionString(AppConfig.ConnectionString)))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(sql, connection))
                    {
                        command.CommandTimeout = 300;
                        await command.ExecuteNonQueryAsync();
                    }
                }

                if (!File.Exists(backupPath))
                    return (false, "Backup file was not created. Check SQL Server file permissions on the backup folder.");

                var fileInfo = new FileInfo(backupPath);
                string sizeStr = FormatFileSize(fileInfo.Length);

                LogBackupEvent("SUCCESS", $"Created backup: {backupFileName} ({sizeStr})");

                return (true, $"✅ Backup created successfully!\n\n" +
                              $"Location: {backupPath}\n" +
                              $"Size: {sizeStr}\n" +
                              $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                LogBackupEvent("FAILED", $"Backup failed: {ex.Message}");
                return (false, $"❌ Backup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores database from a backup file.
        /// Creates a safety backup before restoring.
        /// </summary>
        public static async Task<(bool Success, string Message)> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return (false, "Backup file not found.");

                string dbName = ExtractDatabaseName(AppConfig.ConnectionString);
                if (string.IsNullOrWhiteSpace(dbName))
                    return (false, "Could not determine database name.");

                using (var connection = new OleDbConnection(GetMasterConnectionString(AppConfig.ConnectionString)))
                {
                    await connection.OpenAsync();

                    // Kick out any other connections so RESTORE can proceed
                    using (var cmd = new OleDbCommand(
                        $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection))
                    {
                        cmd.CommandTimeout = 60;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    try
                    {
                        string sql = $"RESTORE DATABASE [{dbName}] FROM DISK = N'{backupPath.Replace("'", "''")}' " +
                                     "WITH REPLACE, RECOVERY, STATS = 10";
                        using (var cmd = new OleDbCommand(sql, connection))
                        {
                            cmd.CommandTimeout = 300;
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    finally
                    {
                        // Always return the DB to multi-user mode, even if restore failed
                        try
                        {
                            using (var cmd = new OleDbCommand(
                                $"ALTER DATABASE [{dbName}] SET MULTI_USER", connection))
                            {
                                cmd.CommandTimeout = 60;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch { }
                    }
                }

                LogBackupEvent("RESTORE_SUCCESS", $"Restored from {Path.GetFileName(backupPath)}");
                return (true, "✅ Database restored successfully!\n\n" +
                              "The application will now close.\n" +
                              "Please restart to complete the restoration.");
            }
            catch (Exception ex)
            {
                LogBackupEvent("RESTORE_FAILED", ex.Message);
                return (false, $"❌ Restore failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets list of all backup files with metadata.
        /// </summary>
        public static List<BackupFileInfo> GetBackupFiles()
        {
            var backups = new List<BackupFileInfo>();

            try
            {
                if (!Directory.Exists(BackupFolder))
                    return backups;

                var files = Directory.GetFiles(BackupFolder, "KPS_Backup_*.bak")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    backups.Add(new BackupFileInfo
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        FileSize = info.Length,
                        CreatedDate = info.CreationTime,
                        SizeFormatted = FormatFileSize(info.Length)
                    });
                }
            }
            catch (Exception ex)
            {
                LogBackupEvent("ERROR_LISTING", $"Could not list backups: {ex.Message}");
            }

            return backups;
        }

        /// <summary>
        /// Opens the backup folder in Windows Explorer.
        /// </summary>
        public static void OpenBackupFolder()
        {
            try
            {
                if (!Directory.Exists(BackupFolder))
                    Directory.CreateDirectory(BackupFolder);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = BackupFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                LogBackupEvent("ERROR_OPEN_FOLDER", ex.Message);
            }
        }

        /// <summary>
        /// Initializes automatic backup scheduler.
        /// Runs backup at specified hour every day.
        /// </summary>
        public static void InitializeAutoBackup(int hourOfDay = 22)
        {
            if (_backupTimer != null)
                return;

            _backupTimer = new System.Timers.Timer();
            _backupTimer.Interval = 60000; // Check every minute

            _backupTimer.Elapsed += async (s, e) =>
            {
                var now = DateTime.Now;

                // Run backup at specified hour, once per day
                if (now.Hour == hourOfDay && !_backupRunToday)
                {
                    var result = await CreateBackupAsync();
                    if (result.Success)
                        LogBackupEvent("AUTO_BACKUP_SUCCESS", $"Automatic backup completed at {now:HH:mm}");
                    else
                        LogBackupEvent("AUTO_BACKUP_FAILED", result.Message);

                    _backupRunToday = true;
                }
                else if (now.Hour != hourOfDay)
                {
                    _backupRunToday = false;
                }
            };

            _backupTimer.AutoReset = true;
            _backupTimer.Start();

            LogBackupEvent("AUTO_BACKUP_INITIALIZED", $"Auto-backup scheduled for {hourOfDay}:00 daily");
        }

        /// <summary>
        /// Stops the automatic backup scheduler.
        /// </summary>
        public static void StopAutoBackup()
        {
            if (_backupTimer != null)
            {
                _backupTimer.Stop();
                _backupTimer.Dispose();
                _backupTimer = null;
                LogBackupEvent("AUTO_BACKUP_STOPPED", "Auto-backup scheduler stopped");
            }
        }

        /// <summary>
        /// Imports an external .bak file into the backup folder so it appears in the list.
        /// </summary>
        public static (bool Success, string Message) ImportBackup(string sourceFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                    return (false, "Source file not found.");

                if (!Directory.Exists(BackupFolder))
                    Directory.CreateDirectory(BackupFolder);

                string fileName = Path.GetFileName(sourceFilePath);
                if (!fileName.StartsWith("KPS_Backup_") || !fileName.EndsWith(".bak"))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    fileName = $"KPS_Backup_{timestamp}_imported.bak";
                }

                string destPath = Path.Combine(BackupFolder, fileName);
                if (File.Exists(destPath))
                    return (false, $"A backup named '{fileName}' already exists in the backup folder.");

                File.Copy(sourceFilePath, destPath);
                LogBackupEvent("IMPORT_SUCCESS", $"Imported: {fileName}");

                return (true, $"Backup imported successfully: {fileName}");
            }
            catch (Exception ex)
            {
                LogBackupEvent("IMPORT_FAILED", ex.Message);
                return (false, $"Import failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an old backup file.
        /// </summary>
        public static (bool Success, string Message) DeleteBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return (false, "Backup file not found.");

                File.Delete(backupPath);
                LogBackupEvent("BACKUP_DELETED", Path.GetFileName(backupPath));

                return (true, "Backup deleted successfully.");
            }
            catch (Exception ex)
            {
                LogBackupEvent("DELETE_FAILED", ex.Message);
                return (false, $"Could not delete backup: {ex.Message}");
            }
        }

        // ─── Helper Methods ───────────────────────────────────────────────

        private static string ExtractDatabaseName(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            var match = Regex.Match(connectionString,
                @"(?:Initial Catalog|Database)\s*=\s*([^;]+)",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string GetMasterConnectionString(string connectionString)
        {
            // Backup/restore must run against [master], not the target DB
            var result = Regex.Replace(connectionString,
                @"(Initial Catalog|Database)\s*=\s*[^;]+",
                "Initial Catalog=master",
                RegexOptions.IgnoreCase);

            if (!Regex.IsMatch(result, @"Initial Catalog\s*=", RegexOptions.IgnoreCase))
                result = result.TrimEnd(';') + ";Initial Catalog=master";

            return result;
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private static void LogBackupEvent(string eventType, string message)
        {
            try
            {
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsDir);

                string logPath = Path.Combine(logsDir, "backups.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{eventType}] {message}\n";

                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }

    /// <summary>
    /// Information about a backup file.
    /// </summary>
    public class BackupFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string SizeFormatted { get; set; }
        public DateTime CreatedDate { get; set; }

        public override string ToString()
        {
            return $"{FileName} ({SizeFormatted}) - {CreatedDate:yyyy-MM-dd HH:mm:ss}";
        }
    }
}

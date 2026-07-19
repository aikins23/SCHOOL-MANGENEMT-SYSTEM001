using System;
using System.IO;
using Microsoft.Data.SqlClient;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Resolves the desktop SQL connection without requiring credentials in App.config.
    /// Environment configuration takes precedence over the current-user DPAPI file.
    /// </summary>
    public static class DatabaseConnectionSettings
    {
        public const string ConnectionEnvironmentVariable = "NYANSAPO_CONNECTION_STRING";
        public const string ConnectionFileEnvironmentVariable = "NYANSAPO_CONNECTION_FILE";
        public const string EnvironmentNameVariable = "NYANSAPO_ENVIRONMENT";

        public static string ProtectedFilePath
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable(ConnectionFileEnvironmentVariable);
                if (!string.IsNullOrWhiteSpace(configured))
                {
                    return Path.GetFullPath(configured.Trim());
                }

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Nyansapo ERP",
                    "database.connection");
            }
        }

        public static string Resolve(string configuredFallback)
        {
            string connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = ReadProtected();
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuredFallback;
            }

            string normalized = Normalize(connectionString);
            EnsureProductionSecurity(normalized);
            return normalized;
        }

        public static string ReadProtected()
        {
            string path = ProtectedFilePath;
            if (!File.Exists(path)) return string.Empty;

            string protectedValue = File.ReadAllText(path).Trim();
            string connectionString = SecretStorage.Unprotect(protectedValue);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "The protected database connection could not be read for this Windows user. " +
                    "Run Scripts\\Set-NyansapoDesktopConnection.ps1 again while signed in as this user.");
            }

            return connectionString;
        }

        public static void SaveProtected(string connectionString)
        {
            string normalized = Normalize(connectionString);
            EnsureProductionSecurity(normalized);

            string path = ProtectedFilePath;
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, SecretStorage.Protect(normalized));

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }

        public static void ClearProtected()
        {
            string path = ProtectedFilePath;
            if (File.Exists(path)) File.Delete(path);
        }

        public static string Normalize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No database connection is configured. Run Scripts\\Set-NyansapoDesktopConnection.ps1 " +
                    "or set " + ConnectionEnvironmentVariable + ".");
            }

            var builder = new SqlConnectionStringBuilder(
                SqlCommandExtensions.StripProvider(connectionString.Trim()));

            if (string.IsNullOrWhiteSpace(builder.DataSource))
                throw new InvalidOperationException("The database connection does not specify a SQL Server.");
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                throw new InvalidOperationException("The database connection does not specify a database name.");
            if (!builder.IntegratedSecurity && string.IsNullOrWhiteSpace(builder.UserID))
                throw new InvalidOperationException("The database connection does not specify a SQL login or Windows authentication.");
            if (!builder.IntegratedSecurity && string.IsNullOrWhiteSpace(builder.Password))
                throw new InvalidOperationException("The SQL login password is missing from the secure database connection.");

            builder.PersistSecurityInfo = false;
            if (builder.ConnectTimeout <= 0) builder.ConnectTimeout = 8;
            if (string.IsNullOrWhiteSpace(builder.ApplicationName))
                builder.ApplicationName = "Nyansapo School ERP Desktop";

            return builder.ConnectionString;
        }

        public static void EnsureProductionSecurity(string connectionString)
        {
            if (!IsProduction()) return;

            var builder = new SqlConnectionStringBuilder(
                SqlCommandExtensions.StripProvider(connectionString));

            if (!builder.Encrypt)
            {
                throw new InvalidOperationException(
                    "Production database connections must set Encrypt=True.");
            }

            if (builder.TrustServerCertificate)
            {
                throw new InvalidOperationException(
                    "Production database connections must validate the SQL Server certificate " +
                    "with TrustServerCertificate=False.");
            }
        }

        public static bool IsProduction()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable(EnvironmentNameVariable),
                "Production",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}

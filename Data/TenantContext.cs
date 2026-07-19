using KingdomPrep.Shared.Models;
using System;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using MicrosoftSqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using MicrosoftSqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Small helper for tenant-aware repository queries. It stays defensive so tests
    /// and legacy databases without the SchoolId column still work while production
    /// databases are filtered once TenantSchema has upgraded them.
    /// </summary>
    public static class TenantContext
    {
        public static Guid CurrentSchoolId
        {
            get
            {
                try
                {
                    var id = SchoolProfile.SchoolId;
                    return id == Guid.Empty ? Guid.Empty : id;
                }
                catch
                {
                    return Guid.Empty;
                }
            }
        }

        public static async Task<bool> HasSchoolIdColumnAsync(MicrosoftSqlConnection connection, string tableName)
        {
            var safeTable = (tableName ?? "").Replace("'", "''");
            using (var cmd = new MicrosoftSqlCommand($"SELECT COL_LENGTH('{safeTable}', 'SchoolId')", connection))
            {
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        public static string FilterClauseSql(string alias = null)
        {
            string prefix = string.IsNullOrWhiteSpace(alias) ? "" : alias.Trim() + ".";
            return " AND " + prefix + "SchoolId = @SchoolId";
        }

        public static void AddSchoolParameter(MicrosoftSqlCommand command)
        {
            var schoolId = RequireSchoolId();
            if (command.CommandText != null && command.CommandText.Contains("@SchoolId"))
            {
                if (!command.Parameters.Contains("@SchoolId"))
                    command.Parameters.AddWithValue("@SchoolId", schoolId);
                return;
            }

            command.AddPositionalParameter(schoolId);
        }

        public static Guid RequireSchoolId()
        {
            var schoolId = CurrentSchoolId;
            if (schoolId == Guid.Empty)
                throw new InvalidOperationException("School identity has not been configured.");
            return schoolId;
        }
    }
}

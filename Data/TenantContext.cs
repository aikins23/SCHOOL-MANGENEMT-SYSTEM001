using System;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

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

        public static async Task<bool> HasSchoolIdColumnAsync(OleDbConnection connection, string tableName)
        {
            using (var cmd = new OleDbCommand($"SELECT COL_LENGTH('{tableName}', 'SchoolId')", connection))
            {
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
        }

        public static string FilterClause(string alias = null)
        {
            string prefix = string.IsNullOrWhiteSpace(alias) ? "" : alias.Trim() + ".";
            return " AND " + prefix + "SchoolId = ?";
        }

        public static void AddSchoolParameter(OleDbCommand command)
        {
            command.Parameters.AddWithValue("?", CurrentSchoolId);
        }
    }
}

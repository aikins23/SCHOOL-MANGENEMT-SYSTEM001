using System;
using System.Data;
using System.Text.RegularExpressions;
using MicrosoftSqlCommand = Microsoft.Data.SqlClient.SqlCommand;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public static class SqlCommandExtensions
    {
        public static string StripProvider(string connectionString)
        {
            try
            {
                var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString };
                builder.Remove("Provider");
                if (!builder.ContainsKey("Connect Timeout") && !builder.ContainsKey("Connection Timeout"))
                {
                    builder["Connect Timeout"] = "8";
                }
                return builder.ConnectionString;
            }
            catch
            {
                return connectionString;
            }
        }

        public static string ConvertPositionalQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return query;
            int counter = 0;
            return Regex.Replace(query, @"\?", match => "@p" + (counter++));
        }

        public static void AddPositionalParameter(this MicrosoftSqlCommand cmd, object value)
        {
            if (cmd.CommandText != null && cmd.CommandText.Contains("?"))
            {
                cmd.CommandText = ConvertPositionalQuery(cmd.CommandText);
            }

            int pIndex = 0;
            while (cmd.Parameters.Contains("@p" + pIndex)) pIndex++;

            string pName = "@p" + pIndex;
            cmd.Parameters.AddWithValue(pName, value ?? DBNull.Value);
        }

        public static void AddPositionalParameter(this MicrosoftSqlCommand cmd, object value, SqlDbType dbType)
        {
            if (cmd.CommandText != null && cmd.CommandText.Contains("?"))
            {
                cmd.CommandText = ConvertPositionalQuery(cmd.CommandText);
            }

            int pIndex = 0;
            while (cmd.Parameters.Contains("@p" + pIndex)) pIndex++;

            string pName = "@p" + pIndex;
            var p = cmd.Parameters.Add(pName, dbType);
            p.Value = value ?? DBNull.Value;
        }

        public static void AddNamedParameter(this MicrosoftSqlCommand cmd, string name, object value)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        public static void AddNamedParameter(this MicrosoftSqlCommand cmd, string name, object value, SqlDbType dbType)
        {
            var p = cmd.Parameters.Add(name, dbType);
            p.Value = value ?? DBNull.Value;
        }

    }
}

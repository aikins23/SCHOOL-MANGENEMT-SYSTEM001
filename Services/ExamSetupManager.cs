using KingdomPrep.Shared.Models;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class ExamSetupInfo
    {
        public string Term { get; set; }
        public string Year { get; set; }
        public int? ExamTypeId { get; set; }
        public string ExamTypeName { get; set; }
        public int? AssessmentNumber { get; set; }
        public string AssessmentLabel { get; set; }
        public decimal ClassWeight { get; set; }
        public decimal ExamWeight { get; set; }
    }

    public static class ExamSetupManager
    {
        public static async Task<ExamSetupInfo> GetActiveSetupAsync()
        {
            var examTypes = new ExamTypeRepository();
            await examTypes.EnsureTableAsync();
            await examTypes.SeedSystemTypesAsync();

            using (var conn = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    SELECT TOP 1 es.Term, es.[Year], es.ExamTypeId, COALESCE(et.Name, 'End of Term') AS ExamTypeName,
                           es.AssessmentNumber, COALESCE(es.AssessmentLabel, et.Name, 'End of Term') AS AssessmentLabel,
                           es.ClassWeight, es.ExamWeight
                    FROM ExamSetups es
                    LEFT JOIN ExamTypes et ON et.ExamTypeId = es.ExamTypeId
                    WHERE es.EndDate >= ?
                    ORDER BY es.StartDate ASC", conn);
                cmd.AddPositionalParameter(DateTime.Today);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ExamSetupInfo
                        {
                            Term = reader["Term"].ToString(),
                            Year = reader["Year"].ToString(),
                            ExamTypeId = reader["ExamTypeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ExamTypeId"]),
                            ExamTypeName = reader["ExamTypeName"].ToString(),
                            AssessmentNumber = reader["AssessmentNumber"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["AssessmentNumber"]),
                            AssessmentLabel = reader["AssessmentLabel"].ToString(),
                            ClassWeight = Convert.ToDecimal(reader["ClassWeight"]),
                            ExamWeight = Convert.ToDecimal(reader["ExamWeight"])
                        };
                    }
                }
            }
            return null; // No active setup found
        }
    }
}

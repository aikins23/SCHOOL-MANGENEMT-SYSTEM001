using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Stores pending admissions (student details + photo + payment amounts) until
    /// the bursar approves. A row exists only while pending; approval/rejection
    /// deletes it. SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class DraftAdmissionRepository : IDraftAdmissionRepository
    {
        private readonly string _connectionString;

        public DraftAdmissionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"IF OBJECT_ID(N'DraftAdmissions', N'U') IS NULL
                    CREATE TABLE DraftAdmissions (
                        DraftID INT IDENTITY(1,1) PRIMARY KEY,
                        FirstName NVARCHAR(100), LastName NVARCHAR(100), DOB DATETIME,
                        Gender NVARCHAR(20), ClassID NVARCHAR(50), Email NVARCHAR(150),
                        HomeTown NVARCHAR(150), Residence NVARCHAR(150), Allegies NVARCHAR(255),
                        EmergencyConatct NVARCHAR(50), GuidanceName NVARCHAR(150),
                        GuidianceEmail NVARCHAR(150), Guidiance_Location NVARCHAR(150),
                        admission_date DATETIME, Std_pic VARBINARY(MAX),
                        AdmissionFee MONEY, SchoolFeePaid MONEY, TermTotal MONEY,
                        PaymentMode NVARCHAR(50), SubmittedBy NVARCHAR(100), SubmittedDate DATETIME);";
                using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();

                using (var alter = new OleDbCommand(
                    "IF COL_LENGTH('DraftAdmissions','BusRouteId') IS NULL ALTER TABLE DraftAdmissions ADD BusRouteId INT NULL", c))
                    await alter.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> AddAsync(DraftAdmission d)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO DraftAdmissions
                    (FirstName,LastName,DOB,Gender,ClassID,Email,HomeTown,Residence,Allegies,
                     EmergencyConatct,GuidanceName,GuidianceEmail,Guidiance_Location,admission_date,Std_pic,
                     AdmissionFee,SchoolFeePaid,TermTotal,PaymentMode,SubmittedBy,SubmittedDate,BusRouteId)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", d.FirstName ?? "");
                    cmd.Parameters.AddWithValue("?", d.LastName ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(d.DateOfBirth));
                    cmd.Parameters.AddWithValue("?", d.Gender ?? "");
                    cmd.Parameters.AddWithValue("?", d.ClassID ?? "");
                    cmd.Parameters.AddWithValue("?", d.Email ?? "");
                    cmd.Parameters.AddWithValue("?", d.HomeTown ?? "");
                    cmd.Parameters.AddWithValue("?", d.Residence ?? "");
                    cmd.Parameters.AddWithValue("?", d.Allergies ?? "");
                    cmd.Parameters.AddWithValue("?", d.EmergencyContact ?? "");
                    cmd.Parameters.AddWithValue("?", d.GuardianName ?? "");
                    cmd.Parameters.AddWithValue("?", d.GuardianEmail ?? "");
                    cmd.Parameters.AddWithValue("?", d.GuardianLocation ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(d.AdmissionDate));
                    cmd.Parameters.Add("?", OleDbType.VarBinary).Value = (object)d.ProfilePhoto ?? new byte[0];
                    cmd.Parameters.AddWithValue("?", d.AdmissionFee);
                    cmd.Parameters.AddWithValue("?", d.SchoolFeePaid);
                    cmd.Parameters.AddWithValue("?", d.TermTotal);
                    cmd.Parameters.AddWithValue("?", d.PaymentMode ?? "Cash");
                    cmd.Parameters.AddWithValue("?", d.SubmittedBy ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(d.SubmittedDate));
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = (object)d.BusRouteId ?? DBNull.Value;
                    await cmd.ExecuteNonQueryAsync();
                    using (var idCmd = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idCmd.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task<IEnumerable<DraftAdmission>> GetPendingAsync()
        {
            var list = new List<DraftAdmission>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT * FROM DraftAdmissions ORDER BY SubmittedDate", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(Map(r));
            }
            return list;
        }

        public async Task<int> CountPendingAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM DraftAdmissions", c))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
        }

        public async Task<DraftAdmission> GetByIdAsync(int draftId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT * FROM DraftAdmissions WHERE DraftID = ?", c))
                {
                    cmd.Parameters.AddWithValue("?", draftId);
                    using (var r = await cmd.ExecuteReaderAsync())
                        return await r.ReadAsync() ? Map(r) : null;
                }
            }
        }

        public async Task<bool> DeleteAsync(int draftId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("DELETE FROM DraftAdmissions WHERE DraftID = ?", c))
                {
                    cmd.Parameters.AddWithValue("?", draftId);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // SQL Server 'datetime' rejects sub-second precision from MSOLEDBSQL; drop it.
        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

        private static bool HasCol(IDataRecord r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static DraftAdmission Map(IDataRecord r) => new DraftAdmission
        {
            DraftID = Convert.ToInt32(r["DraftID"]),
            FirstName = r["FirstName"]?.ToString(),
            LastName = r["LastName"]?.ToString(),
            DateOfBirth = Convert.ToDateTime(r["DOB"]),
            Gender = r["Gender"]?.ToString(),
            ClassID = r["ClassID"]?.ToString(),
            Email = r["Email"]?.ToString(),
            HomeTown = r["HomeTown"]?.ToString(),
            Residence = r["Residence"]?.ToString(),
            Allergies = r["Allegies"]?.ToString(),
            EmergencyContact = r["EmergencyConatct"]?.ToString(),
            GuardianName = r["GuidanceName"]?.ToString(),
            GuardianEmail = r["GuidianceEmail"]?.ToString(),
            GuardianLocation = r["Guidiance_Location"]?.ToString(),
            AdmissionDate = Convert.ToDateTime(r["admission_date"]),
            ProfilePhoto = r["Std_pic"] as byte[],
            AdmissionFee = Convert.ToDecimal(r["AdmissionFee"]),
            SchoolFeePaid = Convert.ToDecimal(r["SchoolFeePaid"]),
            TermTotal = Convert.ToDecimal(r["TermTotal"]),
            PaymentMode = r["PaymentMode"]?.ToString(),
            SubmittedBy = r["SubmittedBy"]?.ToString(),
            SubmittedDate = Convert.ToDateTime(r["SubmittedDate"]),
            BusRouteId = HasCol(r, "BusRouteId") && r["BusRouteId"] != DBNull.Value ? Convert.ToInt32(r["BusRouteId"]) : (int?)null
        };
    }
}

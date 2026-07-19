using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Stores pending admissions (student details + photo + payment amounts) until
    /// the bursar approves. A row exists only while pending; approval/rejection
    /// deletes it. SQL Server via Microsoft.Data.SqlClient.
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
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
                using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();

                using (var alter = new SqlCommand(
                    @"
IF COL_LENGTH('DraftAdmissions','BusRouteId') IS NULL ALTER TABLE DraftAdmissions ADD BusRouteId INT NULL;
IF COL_LENGTH('DraftAdmissions','SchoolId') IS NULL ALTER TABLE DraftAdmissions ADD SchoolId UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('DraftAdmissions','SyncId') IS NULL ALTER TABLE DraftAdmissions ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_DraftAdmissions_SyncId DEFAULT NEWID();
IF COL_LENGTH('DraftAdmissions','UpdatedAt') IS NULL ALTER TABLE DraftAdmissions ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_DraftAdmissions_UpdatedAt DEFAULT SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DraftAdmissions_School_SubmittedDate' AND object_id = OBJECT_ID(N'DraftAdmissions'))
    CREATE INDEX IX_DraftAdmissions_School_SubmittedDate ON DraftAdmissions(SchoolId, SubmittedDate);", c))
                    await alter.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> AddAsync(DraftAdmission d)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, "DraftAdmissions");
                var sql = tenant
                    ? @"
                    DECLARE @InsertedDrafts TABLE (DraftID INT);

                    INSERT INTO DraftAdmissions
                    (FirstName,LastName,DOB,Gender,ClassID,Email,HomeTown,Residence,Allegies,
                     EmergencyConatct,GuidanceName,GuidianceEmail,Guidiance_Location,admission_date,Std_pic,
                     AdmissionFee,SchoolFeePaid,TermTotal,PaymentMode,SubmittedBy,SubmittedDate,BusRouteId,SchoolId)
                    OUTPUT INSERTED.DraftID INTO @InsertedDrafts
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);

                    SELECT TOP 1 DraftID FROM @InsertedDrafts;"
                    : @"
                    DECLARE @InsertedDrafts TABLE (DraftID INT);

                    INSERT INTO DraftAdmissions
                    (FirstName,LastName,DOB,Gender,ClassID,Email,HomeTown,Residence,Allegies,
                     EmergencyConatct,GuidanceName,GuidianceEmail,Guidiance_Location,admission_date,Std_pic,
                     AdmissionFee,SchoolFeePaid,TermTotal,PaymentMode,SubmittedBy,SubmittedDate,BusRouteId)
                    OUTPUT INSERTED.DraftID INTO @InsertedDrafts
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);

                    SELECT TOP 1 DraftID FROM @InsertedDrafts;";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(d.FirstName ?? "");
                    cmd.AddPositionalParameter(d.LastName ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(d.DateOfBirth));
                    cmd.AddPositionalParameter(d.Gender ?? "");
                    cmd.AddPositionalParameter(d.ClassID ?? "");
                    cmd.AddPositionalParameter(d.Email ?? "");
                    cmd.AddPositionalParameter(d.HomeTown ?? "");
                    cmd.AddPositionalParameter(d.Residence ?? "");
                    cmd.AddPositionalParameter(d.Allergies ?? "");
                    cmd.AddPositionalParameter(d.EmergencyContact ?? "");
                    cmd.AddPositionalParameter(d.GuardianName ?? "");
                    cmd.AddPositionalParameter(d.GuardianEmail ?? "");
                    cmd.AddPositionalParameter(d.GuardianLocation ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(d.AdmissionDate));
                    cmd.AddPositionalParameter((object)d.ProfilePhoto ?? new byte[0], SqlDbType.VarBinary);
                    cmd.AddPositionalParameter(d.AdmissionFee);
                    cmd.AddPositionalParameter(d.SchoolFeePaid);
                    cmd.AddPositionalParameter(d.TermTotal);
                    cmd.AddPositionalParameter(d.PaymentMode ?? "Cash");
                    cmd.AddPositionalParameter(d.SubmittedBy ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(d.SubmittedDate));
                    cmd.AddPositionalParameter((object)d.BusRouteId ?? DBNull.Value, SqlDbType.Int);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var id = await cmd.ExecuteScalarAsync();
                    var draftId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    if (draftId > 0 && !tenant)
                    {
                        await StampTenantAsync(c, draftId);
                    }
                    if (draftId > 0)
                    {
                        await TryRecordSyncUpsertAsync(draftId, "Insert");
                    }

                    return draftId;
                }
            }
        }

        public async Task<IEnumerable<DraftAdmission>> GetPendingAsync()
        {
            var list = new List<DraftAdmission>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, "DraftAdmissions");
                var sql = "SELECT * FROM DraftAdmissions";
                if (tenant) sql += " WHERE (SchoolId = @SchoolId OR SchoolId IS NULL)";
                sql += " ORDER BY SubmittedDate";
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(Map(r));
                }
            }
            return list;
        }

        public async Task<int> CountPendingAsync()
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, "DraftAdmissions");
                var sql = "SELECT COUNT(*) FROM DraftAdmissions";
                if (tenant) sql += " WHERE (SchoolId = @SchoolId OR SchoolId IS NULL)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
        }

        public async Task<DraftAdmission> GetByIdAsync(int draftId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, "DraftAdmissions");
                var sql = "SELECT * FROM DraftAdmissions WHERE DraftID = ?";
                if (tenant) sql += " AND (SchoolId = @SchoolId OR SchoolId IS NULL)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(draftId);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    using (var r = await cmd.ExecuteReaderAsync())
                        return await r.ReadAsync() ? Map(r) : null;
                }
            }
        }

        public async Task<bool> DeleteAsync(int draftId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await TryRecordSyncDeleteAsync(draftId);
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, "DraftAdmissions");
                var sql = "DELETE FROM DraftAdmissions WHERE DraftID = ?";
                if (tenant) sql += " AND (SchoolId = @SchoolId OR SchoolId IS NULL)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(draftId);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        private static async Task StampTenantAsync(SqlConnection connection, int draftId)
        {
            if (!await TenantContext.HasSchoolIdColumnAsync(connection, "DraftAdmissions")) return;

            using (var command = new SqlCommand(@"
UPDATE DraftAdmissions
SET SchoolId = @p0,
    UpdatedAt = SYSUTCDATETIME()
WHERE DraftID = @p1
  AND SchoolId IS NULL;", connection))
            {
                TenantContext.AddSchoolParameter(command);
                command.AddPositionalParameter(draftId);
                await command.ExecuteNonQueryAsync();
            }
        }

        // SQL Server 'datetime' rejects sub-second precision from SQL Server; drop it.
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

        private async Task TryRecordSyncUpsertAsync(int draftId, string operation)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync("DraftAdmissions", "DraftID", draftId, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Draft admission sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(int draftId)
        {
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync("DraftAdmissions", "DraftID", draftId);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Draft admission delete sync capture skipped: " + ex.Message);
            }
        }
    }
}

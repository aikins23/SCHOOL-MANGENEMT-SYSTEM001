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
    /// Buses + routes configuration. SQL Server via Microsoft.Data.SqlClient. No DB-level FK; the
    /// bus-route link is guarded in code (a bus assigned to a route cannot be deleted).
    /// </summary>
    public class TransportRepository : ITransportRepository
    {
        private readonly string _connectionString;
        private const string BusesTable = "Buses";
        private const string RoutesTable = "BusRoutes";
        private const string StudentTransportTable = "StudentTransport";
        private const string TransportPaymentTable = "TransportPayment";
        private const string ReminderLogTable = "TransportReminderLog";
        private const string StudentsTable = "Students";
        private const string AttendanceTable = "Attendance";

        public TransportRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string buses = @"IF OBJECT_ID(N'Buses', N'U') IS NULL
                    CREATE TABLE Buses (
                        BusId INT IDENTITY(1,1) PRIMARY KEY,
                        Label NVARCHAR(100) NOT NULL, RegNumber NVARCHAR(40),
                        DriverName NVARCHAR(120), DriverContact NVARCHAR(40),
                        Capacity INT NOT NULL DEFAULT (0), Notes NVARCHAR(255), AddedDate DATETIME);";
                using (var cmd = new SqlCommand(buses, c)) await cmd.ExecuteNonQueryAsync();

                const string routes = @"IF OBJECT_ID(N'BusRoutes', N'U') IS NULL
                    CREATE TABLE BusRoutes (
                        RouteId INT IDENTITY(1,1) PRIMARY KEY,
                        RouteName NVARCHAR(120) NOT NULL, Fee MONEY NOT NULL DEFAULT (0),
                        PaymentTerm NVARCHAR(20) NOT NULL DEFAULT ('Monthly'),
                        BusId INT NULL, Stops NVARCHAR(255), Notes NVARCHAR(255));";
                using (var cmd = new SqlCommand(routes, c)) await cmd.ExecuteNonQueryAsync();

                const string stTransport = @"IF OBJECT_ID(N'StudentTransport', N'U') IS NULL
                    CREATE TABLE StudentTransport (
                        StudentID INT NOT NULL PRIMARY KEY,
                        RouteId INT NOT NULL);";
                using (var cmd = new SqlCommand(stTransport, c)) await cmd.ExecuteNonQueryAsync();

                const string tPay = @"IF OBJECT_ID(N'TransportPayment', N'U') IS NULL
                    CREATE TABLE TransportPayment (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        StudentID INT NOT NULL, RouteId INT NOT NULL,
                        Period NVARCHAR(20) NOT NULL, PeriodStart DATETIME NOT NULL, PeriodEnd DATETIME NOT NULL,
                        AmountPaid MONEY NOT NULL DEFAULT (0), PaymentDate DATETIME NOT NULL,
                        Cashier NVARCHAR(120), Notes NVARCHAR(255));";
                using (var cmd = new SqlCommand(tPay, c)) await cmd.ExecuteNonQueryAsync();

                const string tRem = @"IF OBJECT_ID(N'TransportReminderLog', N'U') IS NULL
                    CREATE TABLE TransportReminderLog (
                        StudentID INT NOT NULL, Period NVARCHAR(20) NOT NULL, SentDate DATETIME NOT NULL,
                        CONSTRAINT PK_TransportReminderLog PRIMARY KEY (StudentID, Period));";
                using (var cmd = new SqlCommand(tRem, c)) await cmd.ExecuteNonQueryAsync();
            }

            await TenantSchema.EnsureTenantColumnsAsync(_connectionString);
        }

        public async Task<List<Bus>> GetBusesAsync(string search)
        {
            var list = new List<Bus>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, BusesTable);
                string sql = "SELECT * FROM Buses WHERE 1=1";
                bool s = !string.IsNullOrWhiteSpace(search);
                if (tenant) sql += TenantContext.FilterClauseSql();
                if (s) sql += " AND (Label LIKE ? OR RegNumber LIKE ? OR DriverName LIKE ?)";
                sql += " ORDER BY Label";
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    if (s)
                    {
                        string like = "%" + search.Trim() + "%";
                        cmd.AddPositionalParameter(like);
                        cmd.AddPositionalParameter(like);
                        cmd.AddPositionalParameter(like);
                    }
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(MapBus(r));
                }
            }
            return list;
        }

        public async Task<int> AddBusAsync(Bus b)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, BusesTable);
                var sql = tenant
                    ? @"INSERT INTO Buses (Label,RegNumber,DriverName,DriverContact,Capacity,Notes,AddedDate,SchoolId)
                    VALUES (?,?,?,?,?,?,?,?)"
                    : @"INSERT INTO Buses (Label,RegNumber,DriverName,DriverContact,Capacity,Notes,AddedDate)
                    VALUES (?,?,?,?,?,?,?)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(b.Label ?? "");
                    cmd.AddPositionalParameter(b.RegNumber ?? "");
                    cmd.AddPositionalParameter(b.DriverName ?? "");
                    cmd.AddPositionalParameter(b.DriverContact ?? "");
                    cmd.AddPositionalParameter(b.Capacity);
                    cmd.AddPositionalParameter(b.Notes ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new SqlCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        var busId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                        await TryRecordSyncUpsertAsync(BusesTable, "BusId", busId, "Insert");
                        return busId;
                    }
                }
            }
        }

        public async Task UpdateBusAsync(Bus b)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, BusesTable);
                var sql = @"UPDATE Buses SET Label=?,RegNumber=?,DriverName=?,DriverContact=?,Capacity=?,Notes=? WHERE BusId=?";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(b.Label ?? "");
                    cmd.AddPositionalParameter(b.RegNumber ?? "");
                    cmd.AddPositionalParameter(b.DriverName ?? "");
                    cmd.AddPositionalParameter(b.DriverContact ?? "");
                    cmd.AddPositionalParameter(b.Capacity);
                    cmd.AddPositionalParameter(b.Notes ?? "");
                    cmd.AddPositionalParameter(b.BusId);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows > 0) await TryRecordSyncUpsertAsync(BusesTable, "BusId", b.BusId, "Update");
                }
            }
        }

        public async Task<bool> DeleteBusAsync(int busId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var busTenant = await TenantContext.HasSchoolIdColumnAsync(c, BusesTable);
                var routeTenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                int used;
                var usedSql = "SELECT COUNT(*) FROM BusRoutes WHERE BusId=?";
                if (routeTenant) usedSql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(usedSql, c))
                {
                    cmd.AddPositionalParameter(busId);
                    if (routeTenant) TenantContext.AddSchoolParameter(cmd);
                    used = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                if (used > 0) return false;
                var delSql = "DELETE FROM Buses WHERE BusId=?";
                if (busTenant) delSql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(delSql, c))
                {
                    await TryRecordSyncDeleteAsync(BusesTable, "BusId", busId);
                    cmd.AddPositionalParameter(busId);
                    if (busTenant) TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task<List<BusRoute>> GetRoutesAsync()
        {
            var list = new List<BusRoute>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var routeTenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var busTenant = await TenantContext.HasSchoolIdColumnAsync(c, BusesTable);
                var sql = @"SELECT r.*, b.Label AS BusLabel FROM BusRoutes r
                    LEFT JOIN Buses b ON r.BusId=b.BusId";
                if (busTenant)
                {
                    sql += TenantContext.FilterClauseSql("b");
                }

                sql += " WHERE 1=1";
                if (routeTenant)
                {
                    sql += TenantContext.FilterClauseSql("r");
                }

                sql += " ORDER BY r.RouteName";
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (busTenant) TenantContext.AddSchoolParameter(cmd);
                    if (routeTenant) TenantContext.AddSchoolParameter(cmd);
                    using (var rd = await cmd.ExecuteReaderAsync())
                        while (await rd.ReadAsync()) list.Add(MapRoute(rd));
                }
            }
            return list;
        }

        public async Task<int> AddRouteAsync(BusRoute r)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var sql = tenant
                    ? @"INSERT INTO BusRoutes (RouteName,Fee,PaymentTerm,BusId,Stops,Notes,SchoolId)
                    VALUES (?,?,?,?,?,?,?)"
                    : @"INSERT INTO BusRoutes (RouteName,Fee,PaymentTerm,BusId,Stops,Notes)
                    VALUES (?,?,?,?,?,?)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(r.RouteName ?? "");
                    cmd.AddPositionalParameter(r.Fee);
                    cmd.AddPositionalParameter(r.PaymentTerm ?? "Monthly");
                    cmd.AddPositionalParameter((object)r.BusId ?? DBNull.Value, SqlDbType.Int);
                    cmd.AddPositionalParameter(r.Stops ?? "");
                    cmd.AddPositionalParameter(r.Notes ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new SqlCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        var routeId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                        await TryRecordSyncUpsertAsync(RoutesTable, "RouteId", routeId, "Insert");
                        return routeId;
                    }
                }
            }
        }

        public async Task UpdateRouteAsync(BusRoute r)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var sql = @"UPDATE BusRoutes SET RouteName=?,Fee=?,PaymentTerm=?,BusId=?,Stops=?,Notes=? WHERE RouteId=?";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(r.RouteName ?? "");
                    cmd.AddPositionalParameter(r.Fee);
                    cmd.AddPositionalParameter(r.PaymentTerm ?? "Monthly");
                    cmd.AddPositionalParameter((object)r.BusId ?? DBNull.Value, SqlDbType.Int);
                    cmd.AddPositionalParameter(r.Stops ?? "");
                    cmd.AddPositionalParameter(r.Notes ?? "");
                    cmd.AddPositionalParameter(r.RouteId);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows > 0) await TryRecordSyncUpsertAsync(RoutesTable, "RouteId", r.RouteId, "Update");
                }
            }
        }

        public async Task<bool> DeleteRouteAsync(int routeId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var sql = "DELETE FROM BusRoutes WHERE RouteId=?";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(sql, c))
                {
                    await TryRecordSyncDeleteAsync(RoutesTable, "RouteId", routeId);
                    cmd.AddPositionalParameter(routeId);
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task SetStudentRouteAsync(int studentId, int? routeId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, StudentTransportTable);
                var delSql = "DELETE FROM StudentTransport WHERE StudentID=?";
                if (tenant) delSql += TenantContext.FilterClauseSql();
                using (var del = new SqlCommand(delSql, c))
                {
                    await TryRecordSyncDeleteAsync(StudentTransportTable, "StudentID", studentId);
                    del.AddPositionalParameter(studentId);
                    if (tenant) TenantContext.AddSchoolParameter(del);
                    await del.ExecuteNonQueryAsync();
                }
                if (routeId.HasValue)
                {
                    var insSql = tenant
                        ? "INSERT INTO StudentTransport (StudentID, RouteId, SchoolId) VALUES (?, ?, ?)"
                        : "INSERT INTO StudentTransport (StudentID, RouteId) VALUES (?, ?)";
                    using (var ins = new SqlCommand(insSql, c))
                    {
                        ins.AddPositionalParameter(studentId);
                        ins.AddPositionalParameter(routeId.Value);
                        if (tenant) TenantContext.AddSchoolParameter(ins);
                        await ins.ExecuteNonQueryAsync();
                        await TryRecordSyncUpsertAsync(StudentTransportTable, "StudentID", studentId, "Insert");
                    }
                }
            }
        }

        public async Task<BusRoute> GetStudentRouteAsync(int studentId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var studentTransportTenant = await TenantContext.HasSchoolIdColumnAsync(c, StudentTransportTable);
                var routeTenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var busTenant = await TenantContext.HasSchoolIdColumnAsync(c, BusesTable);
                var sql = @"SELECT r.*, b.Label AS BusLabel FROM StudentTransport st
                    INNER JOIN BusRoutes r ON st.RouteId = r.RouteId
                    LEFT JOIN Buses b ON r.BusId = b.BusId";
                if (busTenant) sql += TenantContext.FilterClauseSql("b");
                sql += " WHERE st.StudentID = ?";
                if (studentTransportTenant) sql += TenantContext.FilterClauseSql("st");
                if (routeTenant) sql += TenantContext.FilterClauseSql("r");
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (busTenant) TenantContext.AddSchoolParameter(cmd);
                    cmd.AddPositionalParameter(studentId);
                    if (studentTransportTenant) TenantContext.AddSchoolParameter(cmd);
                    if (routeTenant) TenantContext.AddSchoolParameter(cmd);
                    using (var rd = await cmd.ExecuteReaderAsync())
                        return await rd.ReadAsync() ? MapRoute(rd) : null;
                }
            }
        }

        public async Task<bool> AddTransportPaymentAsync(int studentId, int routeId, string periodKey,
            DateTime periodStart, DateTime periodEnd, decimal amountPaid, DateTime date, string cashier, string notes)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, TransportPaymentTable);
                var sql = tenant
                    ? @"INSERT INTO TransportPayment
                    (StudentID,RouteId,Period,PeriodStart,PeriodEnd,AmountPaid,PaymentDate,Cashier,Notes,SchoolId)
                    VALUES (?,?,?,?,?,?,?,?,?,?)"
                    : @"INSERT INTO TransportPayment
                    (StudentID,RouteId,Period,PeriodStart,PeriodEnd,AmountPaid,PaymentDate,Cashier,Notes)
                    VALUES (?,?,?,?,?,?,?,?,?)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(studentId);
                    cmd.AddPositionalParameter(routeId);
                    cmd.AddPositionalParameter(periodKey ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(periodStart));
                    cmd.AddPositionalParameter(TruncateSeconds(periodEnd));
                    cmd.AddPositionalParameter(amountPaid);
                    cmd.AddPositionalParameter(TruncateSeconds(date));
                    cmd.AddPositionalParameter(cashier ?? "");
                    cmd.AddPositionalParameter(notes ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var saved = (await cmd.ExecuteNonQueryAsync()) > 0;
                    if (saved)
                    {
                        var id = await GetLastIdentityAsync(c);
                        await TryRecordSyncUpsertAsync(TransportPaymentTable, "Id", id, "Insert");
                    }
                    return saved;
                }
            }
        }

        public async Task<decimal> GetPaidForPeriodAsync(int studentId, string periodKey)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, TransportPaymentTable);
                var sql = "SELECT ISNULL(SUM(AmountPaid),0) FROM TransportPayment WHERE StudentID=? AND Period=?";
                if (tenant) sql += TenantContext.FilterClauseSql();
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(studentId);
                    cmd.AddPositionalParameter(periodKey ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    var o = await cmd.ExecuteScalarAsync();
                    return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                }
            }
        }

        public async Task<DataTable> GetStudentTransportHistoryAsync(int studentId)
        {
            var dt = new DataTable();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var paymentTenant = await TenantContext.HasSchoolIdColumnAsync(c, TransportPaymentTable);
                var routeTenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var sql = @"SELECT tp.PaymentDate AS [Date], r.RouteName AS [Route], tp.Period AS [Period],
                    tp.AmountPaid AS [Amount Paid], tp.Cashier AS [Cashier], tp.Notes AS [Notes]
                    FROM TransportPayment tp LEFT JOIN BusRoutes r ON tp.RouteId=r.RouteId
                    WHERE tp.StudentID=?";
                if (paymentTenant) sql += TenantContext.FilterClauseSql("tp");
                if (routeTenant) sql += TenantContext.FilterClauseSql("r");
                sql += " ORDER BY tp.PaymentDate DESC, tp.Id DESC";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(studentId);
                    if (paymentTenant) TenantContext.AddSchoolParameter(cmd);
                    if (routeTenant) TenantContext.AddSchoolParameter(cmd);
                    using (var rd = await cmd.ExecuteReaderAsync()) dt.Load(rd);
                }
            }
            return dt;
        }

        // Loads every bus student's route once, then computes period/paid/present-days per row in C#
        // (avoids fragile term-dependent SQL). Daily routes are included only when includeDaily is true.
        private async Task<List<TransportArrear>> BuildArrearsAsync(DateTime asOf, bool includeDaily)
        {
            var rows = new List<TransportArrear>();
            var seed = new List<(int StudentId, int RouteId, string Name, string Phone, string Route, string Term, decimal Fee)>();

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var studentTransportTenant = await TenantContext.HasSchoolIdColumnAsync(c, StudentTransportTable);
                var routeTenant = await TenantContext.HasSchoolIdColumnAsync(c, RoutesTable);
                var studentTenant = await TenantContext.HasSchoolIdColumnAsync(c, StudentsTable);
                var sql = @"SELECT st.StudentID, st.RouteId,
                    (s.FirstName + ' ' + s.LastName) AS StudentName, s.EmergencyConatct AS Phone,
                    r.RouteName, r.PaymentTerm, r.Fee
                    FROM StudentTransport st
                    INNER JOIN BusRoutes r ON st.RouteId = r.RouteId
                    INNER JOIN Students s ON s.StudentID = st.StudentID
                    WHERE 1=1";
                if (studentTransportTenant) sql += TenantContext.FilterClauseSql("st");
                if (routeTenant) sql += TenantContext.FilterClauseSql("r");
                if (studentTenant) sql += TenantContext.FilterClauseSql("s");
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (studentTransportTenant) TenantContext.AddSchoolParameter(cmd);
                    if (routeTenant) TenantContext.AddSchoolParameter(cmd);
                    if (studentTenant) TenantContext.AddSchoolParameter(cmd);
                    using (var rd = await cmd.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                            seed.Add((I(rd["StudentID"]), I(rd["RouteId"]), S(rd["StudentName"]), S(rd["Phone"]),
                                      S(rd["RouteName"]), S(rd["PaymentTerm"]),
                                      rd["Fee"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["Fee"])));
                    }
                }

                foreach (var x in seed)
                {
                    bool daily = !Common.TransportPeriod.SupportsReminders(x.Term);
                    if (daily && !includeDaily) continue;

                    var p = Common.TransportPeriod.Current(x.Term, asOf);

                    decimal paid;
                    var paymentTenant = await TenantContext.HasSchoolIdColumnAsync(c, TransportPaymentTable);
                    var paidSql = "SELECT ISNULL(SUM(AmountPaid),0) FROM TransportPayment WHERE StudentID=? AND Period=?";
                    if (paymentTenant) paidSql += TenantContext.FilterClauseSql();
                    using (var cmd = new SqlCommand(paidSql, c))
                    {
                        cmd.AddPositionalParameter(x.StudentId);
                        cmd.AddPositionalParameter(p.Key);
                        if (paymentTenant) TenantContext.AddSchoolParameter(cmd);
                        var o = await cmd.ExecuteScalarAsync();
                        paid = o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                    }

                    int present;
                    var attendanceTenant = await TenantContext.HasSchoolIdColumnAsync(c, AttendanceTable);
                    var attendanceSql = @"SELECT COUNT(*) FROM Attendance WHERE ReferenceID=? AND ReferenceType='STUDENT'
                          AND [Status]='PRESENT' AND [Date] BETWEEN ? AND ?";
                    if (attendanceTenant) attendanceSql += TenantContext.FilterClauseSql();
                    using (var cmd = new SqlCommand(attendanceSql, c))
                    {
                        cmd.AddPositionalParameter(x.StudentId.ToString());
                        cmd.AddPositionalParameter(p.Start.Date);
                        cmd.AddPositionalParameter(asOf.Date);
                        if (attendanceTenant) TenantContext.AddSchoolParameter(cmd);
                        present = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    rows.Add(new TransportArrear
                    {
                        StudentID = x.StudentId, StudentName = x.Name, GuardianPhone = x.Phone,
                        RouteName = x.Route, Term = x.Term, Fee = x.Fee, Paid = paid, PresentDays = present,
                        Period = p.Key, PeriodStart = p.Start, PeriodEnd = p.End
                    });
                }
            }
            return rows;
        }

        public async Task<List<TransportArrear>> GetArrearsAsync(DateTime asOf, bool includeDaily)
            => await BuildArrearsAsync(asOf, includeDaily);

        public async Task<List<TransportArrear>> GetReminderCandidatesAsync(DateTime asOf)
        {
            var candidates = new List<TransportArrear>();
            foreach (var a in await BuildArrearsAsync(asOf, includeDaily: false))
            {
                if (a.Balance <= 0m || a.PresentDays < 2) continue;
                using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await c.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(c, ReminderLogTable);
                    var sql = "SELECT COUNT(*) FROM TransportReminderLog WHERE StudentID=? AND Period=?";
                    if (tenant) sql += TenantContext.FilterClauseSql();
                    using (var cmd = new SqlCommand(sql, c))
                    {
                        cmd.AddPositionalParameter(a.StudentID);
                        cmd.AddPositionalParameter(a.Period);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);
                        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0) continue;
                    }
                }
                candidates.Add(a);
            }
            return candidates;
        }

        public async Task LogReminderSentAsync(int studentId, string periodKey)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, ReminderLogTable);
                var sql = tenant
                    ? @"IF NOT EXISTS (SELECT 1 FROM TransportReminderLog WHERE StudentID=? AND Period=? AND SchoolId=?)
                    INSERT INTO TransportReminderLog (StudentID,Period,SentDate,SchoolId) VALUES (?,?,?,?)"
                    : @"IF NOT EXISTS (SELECT 1 FROM TransportReminderLog WHERE StudentID=? AND Period=?)
                    INSERT INTO TransportReminderLog (StudentID,Period,SentDate) VALUES (?,?,?)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(studentId);
                    cmd.AddPositionalParameter(periodKey ?? "");
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    cmd.AddPositionalParameter(studentId);
                    cmd.AddPositionalParameter(periodKey ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    if (tenant) TenantContext.AddSchoolParameter(cmd);
                    await cmd.ExecuteNonQueryAsync();
                    var syncId = await FindReminderSyncIdAsync(c, studentId, periodKey ?? "", tenant);
                    if (syncId != Guid.Empty)
                        await new SyncChangeRecorder(_connectionString).RecordUpsertBySyncIdAsync(ReminderLogTable, syncId, "Insert");
                }
            }
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);
        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
        private static int I(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);

        private static async Task<object> GetLastIdentityAsync(SqlConnection connection)
        {
            using (var cmd = new SqlCommand("SELECT @@IDENTITY", connection))
            {
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : value;
            }
        }

        private async Task TryRecordSyncUpsertAsync(string tableName, string primaryKeyName, object primaryKeyValue, string operation)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(tableName, primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Transport sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(string tableName, string primaryKeyName, object primaryKeyValue)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(tableName, primaryKeyName, primaryKeyValue);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Transport delete sync capture skipped: " + ex.Message);
            }
        }

        private async Task<Guid> FindReminderSyncIdAsync(SqlConnection connection, int studentId, string periodKey, bool tenant)
        {
            var schoolId = TenantContext.RequireSchoolId();
            await SyncSchema.EnsureSyncInfrastructureAsync(connection, schoolId);
            var query = "SELECT TOP 1 SyncId FROM TransportReminderLog WHERE StudentID=? AND Period=?";
            if (tenant) query += TenantContext.FilterClauseSql();
            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.AddPositionalParameter(studentId);
                cmd.AddPositionalParameter(periodKey);
                if (tenant) TenantContext.AddSchoolParameter(cmd);
                var value = await cmd.ExecuteScalarAsync();
                if (value is Guid id) return id;
                return Guid.TryParse(Convert.ToString(value), out var parsed) ? parsed : Guid.Empty;
            }
        }

        private static Bus MapBus(IDataRecord r) => new Bus
        {
            BusId = I(r["BusId"]), Label = S(r["Label"]), RegNumber = S(r["RegNumber"]),
            DriverName = S(r["DriverName"]), DriverContact = S(r["DriverContact"]),
            Capacity = I(r["Capacity"]), Notes = S(r["Notes"]),
            AddedDate = r["AddedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["AddedDate"])
        };

        private static BusRoute MapRoute(IDataRecord r) => new BusRoute
        {
            RouteId = I(r["RouteId"]), RouteName = S(r["RouteName"]),
            Fee = r["Fee"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Fee"]),
            PaymentTerm = S(r["PaymentTerm"]),
            BusId = r["BusId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BusId"]),
            BusLabel = S(r["BusLabel"]), Stops = S(r["Stops"]), Notes = S(r["Notes"])
        };
    }
}

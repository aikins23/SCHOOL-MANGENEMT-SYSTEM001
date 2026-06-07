using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Buses + routes configuration. SQL Server (LocalDB) via OleDb. No DB-level FK; the
    /// bus-route link is guarded in code (a bus assigned to a route cannot be deleted).
    /// </summary>
    public class TransportRepository : ITransportRepository
    {
        private readonly string _connectionString;

        public TransportRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string buses = @"IF OBJECT_ID(N'Buses', N'U') IS NULL
                    CREATE TABLE Buses (
                        BusId INT IDENTITY(1,1) PRIMARY KEY,
                        Label NVARCHAR(100) NOT NULL, RegNumber NVARCHAR(40),
                        DriverName NVARCHAR(120), DriverContact NVARCHAR(40),
                        Capacity INT NOT NULL DEFAULT (0), Notes NVARCHAR(255), AddedDate DATETIME);";
                using (var cmd = new OleDbCommand(buses, c)) await cmd.ExecuteNonQueryAsync();

                const string routes = @"IF OBJECT_ID(N'BusRoutes', N'U') IS NULL
                    CREATE TABLE BusRoutes (
                        RouteId INT IDENTITY(1,1) PRIMARY KEY,
                        RouteName NVARCHAR(120) NOT NULL, Fee MONEY NOT NULL DEFAULT (0),
                        PaymentTerm NVARCHAR(20) NOT NULL DEFAULT ('Monthly'),
                        BusId INT NULL, Stops NVARCHAR(255), Notes NVARCHAR(255));";
                using (var cmd = new OleDbCommand(routes, c)) await cmd.ExecuteNonQueryAsync();

                const string stTransport = @"IF OBJECT_ID(N'StudentTransport', N'U') IS NULL
                    CREATE TABLE StudentTransport (
                        StudentID INT NOT NULL PRIMARY KEY,
                        RouteId INT NOT NULL);";
                using (var cmd = new OleDbCommand(stTransport, c)) await cmd.ExecuteNonQueryAsync();

                const string tPay = @"IF OBJECT_ID(N'TransportPayment', N'U') IS NULL
                    CREATE TABLE TransportPayment (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        StudentID INT NOT NULL, RouteId INT NOT NULL,
                        Period NVARCHAR(20) NOT NULL, PeriodStart DATETIME NOT NULL, PeriodEnd DATETIME NOT NULL,
                        AmountPaid MONEY NOT NULL DEFAULT (0), PaymentDate DATETIME NOT NULL,
                        Cashier NVARCHAR(120), Notes NVARCHAR(255));";
                using (var cmd = new OleDbCommand(tPay, c)) await cmd.ExecuteNonQueryAsync();

                const string tRem = @"IF OBJECT_ID(N'TransportReminderLog', N'U') IS NULL
                    CREATE TABLE TransportReminderLog (
                        StudentID INT NOT NULL, Period NVARCHAR(20) NOT NULL, SentDate DATETIME NOT NULL,
                        CONSTRAINT PK_TransportReminderLog PRIMARY KEY (StudentID, Period));";
                using (var cmd = new OleDbCommand(tRem, c)) await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<Bus>> GetBusesAsync(string search)
        {
            var list = new List<Bus>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                string sql = "SELECT * FROM Buses";
                bool s = !string.IsNullOrWhiteSpace(search);
                if (s) sql += " WHERE Label LIKE ? OR RegNumber LIKE ? OR DriverName LIKE ?";
                sql += " ORDER BY Label";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    if (s)
                    {
                        string like = "%" + search.Trim() + "%";
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                    }
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(MapBus(r));
                }
            }
            return list;
        }

        public async Task<int> AddBusAsync(Bus b)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO Buses (Label,RegNumber,DriverName,DriverContact,Capacity,Notes,AddedDate)
                    VALUES (?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", b.Label ?? "");
                    cmd.Parameters.AddWithValue("?", b.RegNumber ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverName ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverContact ?? "");
                    cmd.Parameters.AddWithValue("?", b.Capacity);
                    cmd.Parameters.AddWithValue("?", b.Notes ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task UpdateBusAsync(Bus b)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE Buses SET Label=?,RegNumber=?,DriverName=?,DriverContact=?,Capacity=?,Notes=? WHERE BusId=?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", b.Label ?? "");
                    cmd.Parameters.AddWithValue("?", b.RegNumber ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverName ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverContact ?? "");
                    cmd.Parameters.AddWithValue("?", b.Capacity);
                    cmd.Parameters.AddWithValue("?", b.Notes ?? "");
                    cmd.Parameters.AddWithValue("?", b.BusId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteBusAsync(int busId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                int used;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM BusRoutes WHERE BusId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", busId);
                    used = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                if (used > 0) return false;
                using (var cmd = new OleDbCommand("DELETE FROM Buses WHERE BusId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", busId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task<List<BusRoute>> GetRoutesAsync()
        {
            var list = new List<BusRoute>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT r.*, b.Label AS BusLabel FROM BusRoutes r
                    LEFT JOIN Buses b ON r.BusId=b.BusId ORDER BY r.RouteName";
                using (var cmd = new OleDbCommand(sql, c))
                using (var rd = await cmd.ExecuteReaderAsync())
                    while (await rd.ReadAsync()) list.Add(MapRoute(rd));
            }
            return list;
        }

        public async Task<int> AddRouteAsync(BusRoute r)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO BusRoutes (RouteName,Fee,PaymentTerm,BusId,Stops,Notes)
                    VALUES (?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", r.RouteName ?? "");
                    cmd.Parameters.AddWithValue("?", r.Fee);
                    cmd.Parameters.AddWithValue("?", r.PaymentTerm ?? "Monthly");
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = (object)r.BusId ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("?", r.Stops ?? "");
                    cmd.Parameters.AddWithValue("?", r.Notes ?? "");
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task UpdateRouteAsync(BusRoute r)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE BusRoutes SET RouteName=?,Fee=?,PaymentTerm=?,BusId=?,Stops=?,Notes=? WHERE RouteId=?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", r.RouteName ?? "");
                    cmd.Parameters.AddWithValue("?", r.Fee);
                    cmd.Parameters.AddWithValue("?", r.PaymentTerm ?? "Monthly");
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = (object)r.BusId ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("?", r.Stops ?? "");
                    cmd.Parameters.AddWithValue("?", r.Notes ?? "");
                    cmd.Parameters.AddWithValue("?", r.RouteId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteRouteAsync(int routeId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("DELETE FROM BusRoutes WHERE RouteId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", routeId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task SetStudentRouteAsync(int studentId, int? routeId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var del = new OleDbCommand("DELETE FROM StudentTransport WHERE StudentID=?", c))
                {
                    del.Parameters.AddWithValue("?", studentId);
                    await del.ExecuteNonQueryAsync();
                }
                if (routeId.HasValue)
                {
                    using (var ins = new OleDbCommand("INSERT INTO StudentTransport (StudentID, RouteId) VALUES (?, ?)", c))
                    {
                        ins.Parameters.AddWithValue("?", studentId);
                        ins.Parameters.AddWithValue("?", routeId.Value);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<BusRoute> GetStudentRouteAsync(int studentId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT r.*, b.Label AS BusLabel FROM StudentTransport st
                    INNER JOIN BusRoutes r ON st.RouteId = r.RouteId
                    LEFT JOIN Buses b ON r.BusId = b.BusId WHERE st.StudentID = ?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    using (var rd = await cmd.ExecuteReaderAsync())
                        return await rd.ReadAsync() ? MapRoute(rd) : null;
                }
            }
        }

        public async Task<bool> AddTransportPaymentAsync(int studentId, int routeId, string periodKey,
            DateTime periodStart, DateTime periodEnd, decimal amountPaid, DateTime date, string cashier, string notes)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO TransportPayment
                    (StudentID,RouteId,Period,PeriodStart,PeriodEnd,AmountPaid,PaymentDate,Cashier,Notes)
                    VALUES (?,?,?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", routeId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(periodStart));
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(periodEnd));
                    cmd.Parameters.AddWithValue("?", amountPaid);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(date));
                    cmd.Parameters.AddWithValue("?", cashier ?? "");
                    cmd.Parameters.AddWithValue("?", notes ?? "");
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<decimal> GetPaidForPeriodAsync(int studentId, string periodKey)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT ISNULL(SUM(AmountPaid),0) FROM TransportPayment WHERE StudentID=? AND Period=?", c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    var o = await cmd.ExecuteScalarAsync();
                    return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                }
            }
        }

        public async Task<DataTable> GetStudentTransportHistoryAsync(int studentId)
        {
            var dt = new DataTable();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT tp.PaymentDate AS [Date], r.RouteName AS [Route], tp.Period AS [Period],
                    tp.AmountPaid AS [Amount Paid], tp.Cashier AS [Cashier], tp.Notes AS [Notes]
                    FROM TransportPayment tp LEFT JOIN BusRoutes r ON tp.RouteId=r.RouteId
                    WHERE tp.StudentID=? ORDER BY tp.PaymentDate DESC, tp.Id DESC";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
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

            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT st.StudentID, st.RouteId,
                    (s.FirstName + ' ' + s.LastName) AS StudentName, s.EmergencyContact AS Phone,
                    r.RouteName, r.PaymentTerm, r.Fee
                    FROM StudentTransport st
                    INNER JOIN BusRoutes r ON st.RouteId = r.RouteId
                    INNER JOIN Students s ON s.StudentID = st.StudentID";
                using (var cmd = new OleDbCommand(sql, c))
                using (var rd = await cmd.ExecuteReaderAsync())
                {
                    while (await rd.ReadAsync())
                        seed.Add((I(rd["StudentID"]), I(rd["RouteId"]), S(rd["StudentName"]), S(rd["Phone"]),
                                  S(rd["RouteName"]), S(rd["PaymentTerm"]),
                                  rd["Fee"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["Fee"])));
                }

                foreach (var x in seed)
                {
                    bool daily = !Common.TransportPeriod.SupportsReminders(x.Term);
                    if (daily && !includeDaily) continue;

                    var p = Common.TransportPeriod.Current(x.Term, asOf);

                    decimal paid;
                    using (var cmd = new OleDbCommand(
                        "SELECT ISNULL(SUM(AmountPaid),0) FROM TransportPayment WHERE StudentID=? AND Period=?", c))
                    {
                        cmd.Parameters.AddWithValue("?", x.StudentId);
                        cmd.Parameters.AddWithValue("?", p.Key);
                        var o = await cmd.ExecuteScalarAsync();
                        paid = o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                    }

                    int present;
                    using (var cmd = new OleDbCommand(
                        @"SELECT COUNT(*) FROM Attendance WHERE ReferenceID=? AND ReferenceType='STUDENT'
                          AND [Status]='PRESENT' AND [Date] BETWEEN ? AND ?", c))
                    {
                        cmd.Parameters.AddWithValue("?", x.StudentId.ToString());
                        cmd.Parameters.AddWithValue("?", p.Start.Date);
                        cmd.Parameters.AddWithValue("?", asOf.Date);
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
                using (var c = new OleDbConnection(_connectionString))
                {
                    await c.OpenAsync();
                    using (var cmd = new OleDbCommand(
                        "SELECT COUNT(*) FROM TransportReminderLog WHERE StudentID=? AND Period=?", c))
                    {
                        cmd.Parameters.AddWithValue("?", a.StudentID);
                        cmd.Parameters.AddWithValue("?", a.Period);
                        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0) continue;
                    }
                }
                candidates.Add(a);
            }
            return candidates;
        }

        public async Task LogReminderSentAsync(int studentId, string periodKey)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"IF NOT EXISTS (SELECT 1 FROM TransportReminderLog WHERE StudentID=? AND Period=?)
                    INSERT INTO TransportReminderLog (StudentID,Period,SentDate) VALUES (?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);
        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
        private static int I(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);

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

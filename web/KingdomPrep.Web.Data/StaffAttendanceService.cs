using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IStaffAttendanceService
{
    Task EnsureTableAndColumnsAsync();
    Task<(double Latitude, double Longitude, double RadiusMeters)> GetSchoolGeofenceAsync();
    Task<bool> UpdateSchoolGeofenceAsync(double latitude, double longitude, double radiusMeters);
    double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2);
    Task<StaffAttendanceEntity?> GetTodayRecordAsync(int employeeId);
    Task<List<StaffAttendanceEntity>> GetHistoryAsync(int employeeId, int limit = 30);
    Task<List<StaffAttendanceEntity>> GetMonitorRecordsAsync(DateTime date);
    Task<(bool Success, string Message, StaffAttendanceEntity? Record)> ClockInAsync(int employeeId, string employeeName, string department, double userLat, double userLng);
    Task<(bool Success, string Message, StaffAttendanceEntity? Record)> ClockOutAsync(int employeeId, double userLat, double userLng);
}

public class StaffAttendanceService(IDbContextFactory<AppDbContext> dbFactory) : IStaffAttendanceService
{
    public async Task EnsureTableAndColumnsAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();

        // Add geofencing columns to SchoolInformation table if they do not exist
        try
        {
            const string sqlAlt = @"
                IF COL_LENGTH('SchoolInformation', 'Latitude') IS NULL
                    ALTER TABLE SchoolInformation ADD Latitude FLOAT NOT NULL CONSTRAINT DF_SI_Lat DEFAULT (5.6037);
                IF COL_LENGTH('SchoolInformation', 'Longitude') IS NULL
                    ALTER TABLE SchoolInformation ADD Longitude FLOAT NOT NULL CONSTRAINT DF_SI_Lng DEFAULT (-0.1870);
                IF COL_LENGTH('SchoolInformation', 'GeofenceRadiusMeters') IS NULL
                    ALTER TABLE SchoolInformation ADD GeofenceRadiusMeters FLOAT NOT NULL CONSTRAINT DF_SI_Radius DEFAULT (150.0);
            ";
            await db.Database.ExecuteSqlRawAsync(sqlAlt);
        }
        catch { /* Ignore if already exists or schema restricted */ }

        // Create StaffAttendance table if not exists
        try
        {
            const string sqlTab = @"
                IF OBJECT_ID(N'StaffAttendance', N'U') IS NULL
                CREATE TABLE StaffAttendance (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    EmployeeId INT NOT NULL,
                    EmployeeName NVARCHAR(200) NOT NULL,
                    Department NVARCHAR(100) NOT NULL,
                    Date DATETIME NOT NULL,
                    ClockInTime DATETIME NOT NULL,
                    ClockInLatitude FLOAT NOT NULL,
                    ClockInLongitude FLOAT NOT NULL,
                    ClockInDistanceMeters FLOAT NOT NULL,
                    ClockOutTime DATETIME NULL,
                    ClockOutLatitude FLOAT NULL,
                    ClockOutLongitude FLOAT NULL,
                    ClockOutDistanceMeters FLOAT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    VerificationStatus NVARCHAR(100) NOT NULL,
                    Remarks NVARCHAR(500) NULL,
                    SchoolId UNIQUEIDENTIFIER NULL
                );
            ";
            await db.Database.ExecuteSqlRawAsync(sqlTab);
        }
        catch { /* Ignore if already exists */ }
    }

    public async Task<(double Latitude, double Longitude, double RadiusMeters)> GetSchoolGeofenceAsync()
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();
        var school = await db.SchoolInformation.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
        if (school != null)
        {
            return (school.Latitude, school.Longitude, school.GeofenceRadiusMeters > 0 ? school.GeofenceRadiusMeters : 150.0);
        }
        return (5.6037, -0.1870, 150.0);
    }

    public async Task<bool> UpdateSchoolGeofenceAsync(double latitude, double longitude, double radiusMeters)
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();
        var school = await db.SchoolInformation.FirstOrDefaultAsync(s => s.Id == 1);
        if (school == null) return false;

        school.Latitude = latitude;
        school.Longitude = longitude;
        school.GeofenceRadiusMeters = radiusMeters > 0 ? radiusMeters : 150.0;
        await db.SaveChangesAsync();
        return true;
    }

    public double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task<StaffAttendanceEntity?> GetTodayRecordAsync(int employeeId)
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();
        var today = DateTime.Now.Date;
        return await db.StaffAttendances.AsNoTracking()
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);
    }

    public async Task<List<StaffAttendanceEntity>> GetHistoryAsync(int employeeId, int limit = 30)
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.StaffAttendances.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.Date)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<StaffAttendanceEntity>> GetMonitorRecordsAsync(DateTime date)
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();
        var targetDate = date.Date;
        return await db.StaffAttendances.AsNoTracking()
            .Where(a => a.Date.Date == targetDate)
            .OrderByDescending(a => a.ClockInTime)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, StaffAttendanceEntity? Record)> ClockInAsync(
        int employeeId, string employeeName, string department, double userLat, double userLng)
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();

        var today = DateTime.Now.Date;
        var existing = await db.StaffAttendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);
        if (existing != null)
        {
            return (false, "You have already clocked in today.", existing);
        }

        var (schoolLat, schoolLng, radius) = await GetSchoolGeofenceAsync();
        double distance = CalculateDistanceMeters(schoolLat, schoolLng, userLat, userLng);

        if (distance > radius)
        {
            string msg = $"Geofencing Policy Violation: You are currently {Math.Round(distance)} meters away from school premises. You must be within {Math.Round(radius)} meters to clock in.";
            return (false, msg, null);
        }

        // Server-side timestamp automation
        var now = DateTime.Now;
        string status = now.TimeOfDay > new TimeSpan(8, 0, 0) ? "Present (Late)" : "Present (On Time)";

        var record = new StaffAttendanceEntity
        {
            EmployeeId = employeeId,
            EmployeeName = string.IsNullOrWhiteSpace(employeeName) ? $"Staff #{employeeId}" : employeeName,
            Department = string.IsNullOrWhiteSpace(department) ? "General" : department,
            Date = today,
            ClockInTime = now,
            ClockInLatitude = userLat,
            ClockInLongitude = userLng,
            ClockInDistanceMeters = Math.Round(distance, 1),
            Status = status,
            VerificationStatus = $"Verified On-Premises ({Math.Round(distance)}m)"
        };

        db.StaffAttendances.Add(record);
        await db.SaveChangesAsync();

        return (true, $"Clocked in successfully at {now:hh:mm tt} (Verified {Math.Round(distance)}m from school center).", record);
    }

    public async Task<(bool Success, string Message, StaffAttendanceEntity? Record)> ClockOutAsync(
        int employeeId, double userLat, double userLng)
    {
        await EnsureTableAndColumnsAsync();
        using var db = await dbFactory.CreateDbContextAsync();

        var today = DateTime.Now.Date;
        var record = await db.StaffAttendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

        if (record == null)
        {
            return (false, "You have not clocked in today yet.", null);
        }
        if (record.ClockOutTime.HasValue)
        {
            return (false, $"You already clocked out today at {record.ClockOutTime.Value:hh:mm tt}.", record);
        }

        var (schoolLat, schoolLng, radius) = await GetSchoolGeofenceAsync();
        double distance = CalculateDistanceMeters(schoolLat, schoolLng, userLat, userLng);

        if (distance > radius)
        {
            string msg = $"Geofencing Policy Violation: You are currently {Math.Round(distance)} meters away from school premises. You must be within {Math.Round(radius)} meters to clock out.";
            return (false, msg, record);
        }

        var now = DateTime.Now;
        record.ClockOutTime = now;
        record.ClockOutLatitude = userLat;
        record.ClockOutLongitude = userLng;
        record.ClockOutDistanceMeters = Math.Round(distance, 1);
        if (record.Status == "Present (On Time)") record.Status = "Completed (On Time)";
        else if (record.Status == "Present (Late)") record.Status = "Completed (Late)";

        await db.SaveChangesAsync();

        return (true, $"Clocked out successfully at {now:hh:mm tt} (Verified {Math.Round(distance)}m from school center).", record);
    }
}

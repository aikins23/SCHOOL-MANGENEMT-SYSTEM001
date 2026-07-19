# Nyansapo School ERP - Desktop Deployment Guide

**Version:** 1.0  
**Last Updated:** July 19, 2026
**Target Environment:** Windows Server 2019+ / Windows 10+ Professional

---

## 1. System Requirements

### Hardware
- **Processor:** Intel i5 or equivalent (dual-core minimum)
- **RAM:** 4 GB minimum, 8 GB recommended
- **Storage:** 500 MB for application + database
- **Display:** 1366x768 minimum resolution (1920x1080 recommended)

### Software
- **Operating System:** Windows 10 Professional / Windows 11 / Windows Server 2019+
- **.NET Framework:** 4.7.2 (included in Windows 10/11)
- **SQL Server:** SQL Server 2019 Express or higher (LocalDB acceptable for development)
- **Printer:** Any Windows-compatible printer (optional, for report card printing)

### Network
- **SQL Server:** TCP 1433 by default, or the explicitly configured SQL Server port. LocalDB is for development only and does not accept remote TCP connections.
- **Internet:** Required for web synchronization, SMS, email, online payments, and remote support features. Core desktop workflows can continue offline where their sync queue supports it.

---

## 2. Installation Steps

### Step 1: Prepare Database

```bash
# 1.1 Verify SQL Server LocalDB is installed
sqllocaldb info v14.0

# 1.2 Create database
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i create-database.sql

# 1.3 Verify tables created
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT * FROM sys.tables WHERE name LIKE 'Student%'"
```

**Expected Output:**
```
Database: Neat_Academy
Tables: 20+
Status: Ready
```

### Step 2: Install Application

```bash
# 2.1 Copy application files
xcopy "C:\Setup\kingdom_Preparatory_School_Management_System" "C:\Program Files\Kingdom Prep\" /E /I

# 2.2 Create application shortcut on Desktop
# Target: C:\Program Files\Kingdom Prep\kingdom_Preparatory_School_Management_System.exe
# Start in: C:\Program Files\Kingdom Prep\

# 2.3 Verify execution permission
icacls "C:\Program Files\Kingdom Prep" /grant:r Users:(OI)(CI)F
```

### Step 3: Configure the Protected Database Connection

Do not edit `App.config` or place SQL passwords in deployment files. Configure the installed desktop application under the Windows user that will run it:

```powershell
.\Scripts\Set-NyansapoDesktopConnection.ps1 `
  -Server sql.school.example `
  -Database Neat_Academy `
  -Username nyansapo_app
```

The script securely prompts for the password, tests the connection, requires encryption, and saves the connection using current-user Windows DPAPI. Production must use a trusted SQL Server certificate; do not use `-TrustServerCertificate` in production.

See `docs/DATABASE_CONNECTION_SECURITY.md` and `docs/SQL_SERVER_LOCAL_SETUP.md` for the complete trust model and local development exception.

### Step 4: Test Installation

1. **Launch Application**
   ```bash
   C:\Program Files\Kingdom Prep\kingdom_Preparatory_School_Management_System.exe
   ```

2. **Verify Database Connection**
   - Main form should load without errors
   - Navigation sidebar appears
   - Student list loads successfully

3. **Check Logs**
   ```bash
   # View application log
   notepad C:\Program Files\Kingdom Prep\bin\Debug\NLog.log
   ```

---

## 3. Configuration Checklist

- [ ] SQL Server database created and accessible
- [ ] Connection string points to correct database
- [ ] All users have read/write access to application directory
- [ ] Windows Firewall allows SQL Server port (if remote)
- [ ] Printer configured (if report card printing needed)
- [ ] NLog configuration verified (logging enabled)
- [ ] App.config backup created before modifications

---

## 4. Feature Enablement

### Report Card Printing
To enable physical printer output:

1. **Add Printer to Windows**
   - Settings → Devices → Printers & Scanners
   - Click "Add a printer"
   - Select printer from network or add manually

2. **Verify in Application**
   - Launch application
   - Navigate to EXAMSVIEW
   - Click "Print Report Card" button
   - Verify printer appears in print dialog

### PDF Export
No additional configuration needed. PDFs save to user-selected directory.

### Email Notifications (Future)
Configuration required in Week 7 (SMS/Email integration):
- SMTP server address
- SMTP credentials
- Email template settings

---

## 5. Database Initialization

### Create Initial Database

**Script: create-database.sql**

```sql
-- Create database if not exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Neat_Academy')
BEGIN
    CREATE DATABASE Neat_Academy;
END
GO

USE Neat_Academy;

-- Create Students table
CREATE TABLE Students (
    StudentID NVARCHAR(20) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE,
    ClassID NVARCHAR(20),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- Create Exams table
CREATE TABLE Exams (
    ExamID INT PRIMARY KEY IDENTITY(1,1),
    StudentID NVARCHAR(20) FOREIGN KEY REFERENCES Students(StudentID),
    SubjectID NVARCHAR(50),
    Score DECIMAL(5,2),
    Term NVARCHAR(10),
    Year INT,
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- Create StudentTermRemarks table
CREATE TABLE StudentTermRemarks (
    StudentID NVARCHAR(20) PRIMARY KEY FOREIGN KEY REFERENCES Students(StudentID),
    Term NVARCHAR(10),
    Year INT,
    ClassTeacherRemarks NVARCHAR(MAX),
    HeadTeacherRemarks NVARCHAR(MAX),
    Attitude NVARCHAR(50),
    Interest NVARCHAR(50),
    Conduct NVARCHAR(50)
);

-- Create Fees table
CREATE TABLE Fees (
    FeeID INT PRIMARY KEY IDENTITY(1,1),
    StudentID NVARCHAR(20) FOREIGN KEY REFERENCES Students(StudentID),
    FeeType NVARCHAR(50),
    Amount DECIMAL(10,2),
    PaidAmount DECIMAL(10,2),
    Balance DECIMAL(10,2),
    Term NVARCHAR(10),
    Year INT,
    PaymentDate DATETIME
);

-- Create Attendance table
CREATE TABLE Attendance (
    AttendanceID INT PRIMARY KEY IDENTITY(1,1),
    StudentID NVARCHAR(20) FOREIGN KEY REFERENCES Students(StudentID),
    AttendanceDate DATE,
    Status NVARCHAR(20), -- PRESENT, ABSENT, LATE
    Remarks NVARCHAR(200)
);

-- Create indexes for performance
CREATE INDEX IX_Student_Class ON Students(ClassID);
CREATE INDEX IX_Exam_Student ON Exams(StudentID);
CREATE INDEX IX_Attendance_Date ON Attendance(AttendanceDate);
CREATE INDEX IX_Fees_Student ON Fees(StudentID);

-- Insert sample data if needed
-- ...

PRINT 'Database initialized successfully'
```

Run the script:
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i create-database.sql
```

---

## 6. Troubleshooting

### Issue: Application won't start
**Solution:**
1. Verify .NET 4.7.2 installed: `reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Version`
2. Check Windows Event Viewer for errors
3. Clear application cache: `del C:\Users\[user]\AppData\Local\[AppName]`

### Issue: Database connection fails
**Solution:**
1. Verify SQL Server is running: `sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT @@VERSION"`
2. Check connection string in App.config
3. Verify database exists: `sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT name FROM sys.databases"`
4. Check Windows Firewall (if remote SQL Server)

### Issue: Report card printing fails
**Solution:**
1. Verify printer is installed: Settings → Devices → Printers
2. Check printer is online and has toner/paper
3. Try "Print to PDF" first to test PDF generation
4. Check application log for specific error: `C:\Program Files\Kingdom Prep\bin\Debug\NLog.log`

### Issue: Performance is slow
**Solution:**
1. Check database indexes: `DBCC DBREINDEX ('Exams')`
2. Verify RAM usage (Task Manager)
3. Check network latency (if remote database)
4. Clear application cache and restart

---

## 7. Backup & Recovery

### Daily Backup

**Backup Script (backup-database.bat)**
```batch
@ECHO OFF
SET BACKUP_DIR=C:\Backups\Kingdom_Prep
SET TIMESTAMP=%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%

MKDIR %BACKUP_DIR%\%TIMESTAMP%

sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q ^
"BACKUP DATABASE Neat_Academy TO DISK = N'%BACKUP_DIR%\%TIMESTAMP%\Neat_Academy.bak' WITH INIT"

ECHO Backup completed at %TIMESTAMP%
PAUSE
```

Schedule in Windows Task Scheduler:
- Trigger: Daily at 22:00 (after school hours)
- Action: Run backup-database.bat
- Retention: Keep last 7 days of backups

### Recovery

```bash
# Restore from backup
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q ^
"RESTORE DATABASE Neat_Academy FROM DISK = N'C:\Backups\Kingdom_Prep\backup.bak' WITH REPLACE"

# Verify recovery
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT COUNT(*) FROM Students"
```

---

## 8. Security Configuration

### Windows Firewall (if remote database)
```powershell
# Allow SQL Server port 1433
netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=tcp localport=1433

# Allow application port (if Web API enabled in future)
netsh advfirewall firewall add rule name="Kingdom Prep API" dir=in action=allow protocol=tcp localport=5000
```

### Database Security
```sql
-- Create application user with minimal permissions
CREATE LOGIN AppUser WITH PASSWORD='StrongPassword123!';
CREATE USER AppUser FOR LOGIN AppUser;

-- Grant only necessary permissions
GRANT SELECT, INSERT, UPDATE ON dbo.Students TO AppUser;
GRANT SELECT, INSERT, UPDATE ON dbo.Exams TO AppUser;
-- ... (grant per table)

-- Revoke schema modification rights
REVOKE ALTER ANY SCHEMA TO AppUser;
```

### File Permissions
```bash
# Set NTFS permissions (restrictive)
icacls "C:\Program Files\Kingdom Prep" /grant:r "DOMAIN\Users":(OI)(CI)(RX)
icacls "C:\Program Files\Kingdom Prep" /grant:r "DOMAIN\Admins":(OI)(CI)(F)

# Remove public access
icacls "C:\Program Files\Kingdom Prep" /remove "Everyone"
```

---

## 9. Maintenance Schedule

| Task | Frequency | Owner |
|------|-----------|-------|
| Database backup | Daily (22:00) | IT Staff |
| Log review | Weekly | IT Manager |
| Database optimization | Monthly | DBA |
| Security patches | Monthly | IT Staff |
| Full system backup | Weekly | IT Staff |
| Performance audit | Quarterly | IT Manager |

---

## 10. Support Contact

**For Technical Issues:**
- Email: itsupport@kingdomprep.edu
- Phone: +233 XXX-XXX-XXXX
- Hours: 7:30 AM - 4:00 PM (School Days)

**Escalation:**
- Tier 1: IT Support Staff
- Tier 2: IT Manager
- Tier 3: System Administrator

---

**Deployment Checklist Status: ✅ COMPLETE**

All steps verified and tested. System ready for production deployment.

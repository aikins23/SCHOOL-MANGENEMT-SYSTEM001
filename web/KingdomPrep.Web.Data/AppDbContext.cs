using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

/// <summary>
/// EF Core context over the EXISTING Neat_Academy schema.
/// Do not run EF migrations against this — the desktop app owns the schema.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<StudentEntity> Students => Set<StudentEntity>();
    public DbSet<PaymentRecordEntity> PaymentRecords => Set<PaymentRecordEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<StudentFeeLedgerEntity> StudentFeeLedgers => Set<StudentFeeLedgerEntity>();
    public DbSet<AttendanceEntity> Attendances => Set<AttendanceEntity>();
    public DbSet<StaffAttendanceEntity> StaffAttendances => Set<StaffAttendanceEntity>();
    public DbSet<AssignmentEntity> Assignments => Set<AssignmentEntity>();
    public DbSet<LeaveEntity> Leaves => Set<LeaveEntity>();
    public DbSet<ClassAssignmentEntity> ClassAssignments => Set<ClassAssignmentEntity>();
    public DbSet<DraftAdmissionEntity> DraftAdmissions => Set<DraftAdmissionEntity>();
    public DbSet<ExamResultEntity> Exams => Set<ExamResultEntity>();
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<StudentTermRemarksEntity> StudentTermRemarks => Set<StudentTermRemarksEntity>();
    public DbSet<ParentRequestEntity> ParentRequests => Set<ParentRequestEntity>();
    public DbSet<ApprovalWorkflowEntity> ApprovalWorkflows => Set<ApprovalWorkflowEntity>();
    public DbSet<ClassPerformanceReportEntity> ClassPerformanceReports => Set<ClassPerformanceReportEntity>();
    public DbSet<StudentPerformanceEntryEntity> StudentPerformanceEntries => Set<StudentPerformanceEntryEntity>();
    public DbSet<ExamTypeEntity> ExamTypes => Set<ExamTypeEntity>();
    public DbSet<ExamSetupEntity> ExamSetups => Set<ExamSetupEntity>();
    public DbSet<ClassSubjectEntity> ClassSubjects => Set<ClassSubjectEntity>();
    public DbSet<GradingSchemeEntity> GradingSchemes => Set<GradingSchemeEntity>();
    public DbSet<SchoolInfoEntity> SchoolInformation => Set<SchoolInfoEntity>();
    public DbSet<SchoolPaymentSettingsEntity> SchoolPaymentSettings => Set<SchoolPaymentSettingsEntity>();
    public DbSet<OnlinePaymentIntentEntity> OnlinePaymentIntents => Set<OnlinePaymentIntentEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<AdditionalFeeEntity> AdditionalFees => Set<AdditionalFeeEntity>();
    public DbSet<AdditionalFeeStudentChargeEntity> AdditionalFeeStudentCharges => Set<AdditionalFeeStudentChargeEntity>();
    public DbSet<AcademicTermEntity> AcademicTerms => Set<AcademicTermEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SchoolInfoEntity>().HasKey(s => s.Id);
        b.Entity<SchoolPaymentSettingsEntity>().HasKey(s => s.Id);
        b.Entity<SchoolPaymentSettingsEntity>()
            .HasIndex(s => s.SchoolId)
            .IsUnique();
        b.Entity<OnlinePaymentIntentEntity>().HasKey(i => i.Id);
        b.Entity<OnlinePaymentIntentEntity>()
            .HasIndex(i => i.Reference)
            .IsUnique();

        b.Entity<UserEntity>().HasKey(u => u.Username);

        b.Entity<StudentEntity>().HasKey(s => s.StudentID);

        b.Entity<EmployeeEntity>().HasKey(e => e.EmployeeID);
        b.Entity<PaymentRecordEntity>()
            .HasKey(p => p.ID);
        b.Entity<PaymentRecordEntity>()
            .ToTable("payment_record", tb => tb.HasTrigger("LegacyDesktopTrigger"));

        b.Entity<StudentFeeLedgerEntity>()
            .HasKey(l => new { l.StudentID, l.TermID });
        b.Entity<StudentFeeLedgerEntity>()
            .ToTable("StudentFeeLedger", tb => tb.HasTrigger("LegacyDesktopTrigger"));
        b.Entity<AttendanceEntity>()
            .HasKey(a => a.AttendanceID);
        b.Entity<AttendanceEntity>()
            .ToTable("Attendance", tb => tb.HasTrigger("LegacyDesktopTrigger"));
        b.Entity<StaffAttendanceEntity>()
            .HasKey(a => a.Id);

        b.Entity<AssignmentEntity>().HasKey(a => a.AssignmentID);
        b.Entity<DraftAdmissionEntity>().HasKey(d => d.DraftID);

        b.Entity<ExamResultEntity>()
            .HasKey(e => e.ExamID);
        b.Entity<ExamResultEntity>()
            .ToTable("examss", tb => tb.HasTrigger("LegacyDesktopTrigger"));

        b.Entity<ParentRequestEntity>()
            .HasKey(p => p.RequestID);
        b.Entity<ParentRequestEntity>()
            .ToTable("ParentRequests", tb => tb.HasTrigger("LegacyDesktopTrigger"));

        b.Entity<StudentTermRemarksEntity>()
            .ToTable("StudentTermRemarks", tb => tb.HasTrigger("LegacyDesktopTrigger"))
            .HasKey(r => r.ID);

        b.Entity<ApprovalWorkflowEntity>().HasKey(a => a.WorkflowId);
        b.Entity<ClassPerformanceReportEntity>().HasKey(r => r.ReportId);
        b.Entity<StudentPerformanceEntryEntity>().HasKey(e => e.EntryId);

        b.Entity<ExamSetupEntity>().HasKey(e => e.SetupID);
        b.Entity<ExamTypeEntity>().HasKey(e => e.ExamTypeId);

        b.Entity<ClassSubjectEntity>()
            .ToTable("ClassSubjects", tb => tb.HasTrigger("LegacyDesktopTrigger"))
            .HasKey(c => c.Id);

        b.Entity<AuditLogEntity>().HasKey(a => a.AuditLogID);

        b.Entity<AdditionalFeeEntity>()
            .ToTable("AdditionalFees", tb => tb.HasTrigger("LegacyDesktopTrigger"))
            .HasKey(f => f.AdditionalFeeId);
        b.Entity<AdditionalFeeStudentChargeEntity>()
            .ToTable("AdditionalFeeStudentCharges", tb => tb.HasTrigger("LegacyDesktopTrigger"))
            .HasKey(c => c.ChargeId);

        b.Entity<AcademicTermEntity>()
            .ToTable("AcademicTerms", tb => tb.HasTrigger("LegacyDesktopTrigger"))
            .HasKey(t => t.TermID);
    }
}

using KingdomPrep.Web.Api;

public class SyncTablePolicyTests
{
    [Theory]
    [InlineData("Students")]
    [InlineData("payment_record")]
    [InlineData("ClassSubjects")]
    [InlineData("TimetableEntries")]
    [InlineData("Notices")]
    [InlineData("AcademicYears")]
    [InlineData("AcademicTerms")]
    [InlineData("StudentFeeLedger")]
    [InlineData("ExamTypes")]
    [InlineData("ApprovalWorkflows")]
    [InlineData("ClassPerformanceReports")]
    [InlineData("StudentPerformanceEntries")]
    public void IsAllowed_AcceptsKnownSyncTables(string tableName)
        => Assert.True(SyncTablePolicy.IsAllowed(tableName));

    [Theory]
    [InlineData("")]
    [InlineData("sys.tables")]
    [InlineData("Students; DROP TABLE Users")]
    [InlineData("OtherSchoolPrivateTable")]
    public void IsAllowed_RejectsUnknownOrUnsafeTables(string tableName)
        => Assert.False(SyncTablePolicy.IsAllowed(tableName));
}

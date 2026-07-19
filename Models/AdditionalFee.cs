using System;
using System.Collections.Generic;

namespace kingdom_Preparatory_School_Management_System.Models
{
    public class AdditionalFee
    {
        public int AdditionalFeeId { get; set; }
        public string FeeName { get; set; }
        public string Description { get; set; }
        public string AcademicYear { get; set; }
        public string TermName { get; set; }
        public DateTime? DueDate { get; set; }
        public string AssignmentMode { get; set; }
        public decimal DefaultAmount { get; set; }
        public bool IsCompulsory { get; set; }
        public bool AllowsPartPayment { get; set; }
        public bool NotifyParentsBySms { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectionReason { get; set; }
        public Guid? SchoolId { get; set; }

        public List<AdditionalFeeAmount> Amounts { get; set; } = new List<AdditionalFeeAmount>();
    }

    public class AdditionalFeeAmount
    {
        public int AmountId { get; set; }
        public int AdditionalFeeId { get; set; }
        public string ScopeType { get; set; }
        public string ScopeKey { get; set; }
        public decimal Amount { get; set; }
    }

    public class AdditionalFeeStudentCharge
    {
        public int ChargeId { get; set; }
        public int AdditionalFeeId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime PostedDate { get; set; }
        public string GuardianPhone { get; set; }
    }

    public class AdditionalFeePreview
    {
        public int StudentCount { get; set; }
        public decimal ExpectedTotal { get; set; }
        public List<AdditionalFeeStudentCharge> Students { get; set; } = new List<AdditionalFeeStudentCharge>();
    }

    public static class AdditionalFeeStatuses
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "Pending Approval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string ReturnedForCorrection = "Returned for Correction";
        public const string Active = "Active";
        public const string Closed = "Closed";
        public const string Cancelled = "Cancelled";
    }

    public static class AdditionalFeeAssignmentModes
    {
        public const string Flat = "Flat";
        public const string Class = "Class";
        public const string Department = "Department";
    }

    public static class AdditionalFeeScopeTypes
    {
        public const string All = "All";
        public const string Class = "Class";
        public const string Department = "Department";
    }
}

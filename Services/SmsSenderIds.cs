using KingdomPrep.Shared.Models;
using System;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Builds per-duty alphanumeric SMS Sender IDs from the configured school
    /// abbreviation. GSM caps alphanumeric sender IDs at 11 characters.
    /// </summary>
    public static class SmsSenderIds
    {
        public const string StudentSuffix  = "STDADM";
        public const string EmployeeSuffix = "EMPADM";
        public const string NoticeSuffix   = "NOTICE";
        // Trailing dot matches the sender ID approved on BulkSMSGh ("NSFEES.").
        // NOTE: Arkesel approved "NSFEES" (no dot) — fee SMS via Arkesel will need
        // that ID re-approved as "NSFEES." (or make this provider-specific).
        public const string FeeSuffix      = "FEES.";
        public const int MaxLength = 11;

        public static string Build(string abbreviation, string suffix)
        {
            string abbr = (abbreviation ?? "").Trim().ToUpperInvariant();
            return abbr + suffix;
        }

        /// <summary>True when {abbr}STDADM (the longest duty suffix) would exceed 11 chars.</summary>
        public static bool ExceedsMaxLength(string abbreviation)
        {
            return Build(abbreviation, StudentSuffix).Length > MaxLength;
        }

        public static string StudentAdmission  => Build(AppConfig.Sms.SchoolAbbreviation, StudentSuffix);
        public static string EmployeeAdmission => Build(AppConfig.Sms.SchoolAbbreviation, EmployeeSuffix);
        public static string Notice            => Build(AppConfig.Sms.SchoolAbbreviation, NoticeSuffix);
        public static string FeeReminder       => Build(AppConfig.Sms.SchoolAbbreviation, FeeSuffix);
    }
}

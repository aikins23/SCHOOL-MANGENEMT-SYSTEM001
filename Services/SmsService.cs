using System;
using System.IO;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// SMS facade. Chooses a provider from AppConfig.Sms and exposes per-duty
    /// helpers that attach the correct custom sender ID. All sends are best-effort
    /// and never throw to the caller.
    /// </summary>
    public static class SmsService
    {
        private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        private static ISmsProvider ResolveProvider()
        {
            // Real sending only when enabled, a known provider is selected, and a key exists.
            if (AppConfig.Sms.Enabled && !string.IsNullOrWhiteSpace(AppConfig.Sms.ApiKey))
            {
                string provider = AppConfig.Sms.Provider;
                if (string.Equals(provider, "Arkesel", StringComparison.OrdinalIgnoreCase))
                    return new ArkeselSmsProvider(AppConfig.Sms.ApiKey);
                if (string.Equals(provider, "BulkSMSGh", StringComparison.OrdinalIgnoreCase))
                    return new BulkSmsGhProvider(AppConfig.Sms.ApiKey);
            }
            return new LogSmsProvider();
        }

        /// <summary>Low-level send with an explicit sender ID. Normalizes the GH number first.</summary>
        public static async Task<(bool Success, string Message)> SendSmsAsync(
            string recipient, string message, string senderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return (false, "Message body is required.");

                string normalized = PhoneNumberGh.NormalizeGh(recipient);
                if (normalized == null)
                {
                    Log("SKIPPED", recipient, senderId, "Invalid/blank phone number");
                    return (false, "Invalid recipient phone number.");
                }

                var provider = ResolveProvider();
                var result = await provider.SendAsync(senderId, normalized, message);
                Log(result.Success ? "SENT" : "ERROR", normalized, senderId,
                    $"{provider.Name}: {result.Message}");
                return result;
            }
            catch (Exception ex)
            {
                Log("ERROR", recipient, senderId, ex.Message);
                return (false, $"Failed to send SMS: {ex.Message}");
            }
        }

        public static Task<(bool Success, string Message)> SendStudentAdmissionAsync(
            string recipient, Models.Student student)
        {
            return SendSmsAsync(recipient, BuildStudentAdmissionMessage(student), SmsSenderIds.StudentAdmission);
        }

        /// <summary>
        /// Detailed admission confirmation addressed to the guardian, listing the
        /// recorded student details. Shared by the registration SMS and email so
        /// both channels carry the same content.
        /// </summary>
        public static string BuildStudentAdmissionMessage(Models.Student s)
        {
            string guardian = string.IsNullOrWhiteSpace(s.GuardianName) ? "Guardian" : s.GuardianName.Trim();
            return
$@"Dear {guardian}, your ward {s.FirstName} has been admitted to Kingdom Preparatory School with the following details:
- Student ID: {s.StudentID}
- Name: {s.FullName}
- Class: {s.ClassID}
- Gender: {s.Gender}
- Date of Birth: {s.DateOfBirth:dd/MM/yyyy}
- Admission Date: {s.AdmissionDate:dd/MM/yyyy}

To rectify any details or information, kindly visit or contact the school administrator. Thank you.
- Administrator";
        }

        /// <summary>
        /// Admission confirmation including the bursar-approved payment lines.
        /// Used after a draft admission is approved.
        /// </summary>
        public static string BuildStudentAdmissionMessage(
            Models.Student s, decimal admissionFeePaid, decimal schoolFeePaid, decimal termTotal)
        {
            string guardian = string.IsNullOrWhiteSpace(s.GuardianName) ? "Guardian" : s.GuardianName.Trim();
            return
$@"Dear {guardian}, your ward {s.FirstName} has been admitted to Kingdom Preparatory School with the following details:
- Student ID: {s.StudentID}
- Name: {s.FullName}
- Class: {s.ClassID}
- Gender: {s.Gender}
- Date of Birth: {s.DateOfBirth:dd/MM/yyyy}
- Admission Date: {s.AdmissionDate:dd/MM/yyyy}
- Admission fee paid: GHS {admissionFeePaid:N2}
- School fee paid: GHS {schoolFeePaid:N2} out of GHS {termTotal:N2}

To rectify any details or information, kindly visit or contact the school administrator. Thank you.
- Administrator";
        }

        public static Task<(bool Success, string Message)> SendStudentAdmissionAsync(
            string recipient, Models.Student student, decimal admissionFeePaid, decimal schoolFeePaid, decimal termTotal)
        {
            return SendSmsAsync(recipient,
                BuildStudentAdmissionMessage(student, admissionFeePaid, schoolFeePaid, termTotal),
                SmsSenderIds.StudentAdmission);
        }

        public static Task<(bool Success, string Message)> SendEmployeeAdmissionAsync(
            string recipient, Models.Employee employee)
        {
            return SendSmsAsync(recipient, BuildEmployeeAdmissionMessage(employee), SmsSenderIds.EmployeeAdmission);
        }

        /// <summary>
        /// Detailed employment confirmation addressed to the employee, listing the
        /// recorded details (salary intentionally excluded). Shared by SMS and email.
        /// </summary>
        public static string BuildEmployeeAdmissionMessage(Models.Employee e)
        {
            return
$@"Dear {e.FullName}, you have been registered as an employee at Kingdom Preparatory School with the following details:
- Employee ID: {e.EmployeeID}
- Name: {e.FullName}
- Department: {e.Department}
- Position: {e.Position}
- Employment Date: {e.EmploymentDate:dd/MM/yyyy}

To rectify any details or information, kindly visit or contact the school administrator. Thank you.
- Administrator";
        }

        public static Task<(bool Success, string Message)> SendFeeReminderAsync(
            string recipient, string studentName, decimal balance)
        {
            string message =
$@"Dear Guardian,

This is a reminder that {studentName} has an outstanding balance of GHS {balance:N2}.

Please pay at your earliest convenience.

- Kingdom Preparatory School Accounts";
            return SendSmsAsync(recipient, message, SmsSenderIds.FeeReminder);
        }

        /// <summary>Used by the settings Test button. Uses the student-admission sender ID.</summary>
        public static Task<(bool Success, string Message)> SendTestAsync(string recipient)
        {
            return SendSmsAsync(recipient,
                "Test SMS from Kingdom Preparatory School. Your SMS settings are working.",
                SmsSenderIds.StudentAdmission);
        }

        public static Task<(bool Success, string Message)> SendPaymentReceivedAsync(
            string recipient, string studentName, decimal amountPaid, decimal newBalance)
        {
            string balanceLine = newBalance > 0
                ? $"Outstanding balance: GHS {newBalance:N2}."
                : "Balance fully cleared.";
            string message =
                $"Dear Guardian, payment of GHS {amountPaid:N2} received for {studentName}. " +
                $"{balanceLine} Thank you. - Kingdom Preparatory School Accounts";
            return SendSmsAsync(recipient, message, SmsSenderIds.FeeReminder);
        }

        public static Task<(bool Success, string Message)> SendLeaveSubmittedAsync(
            string recipient, string employeeName)
        {
            string message =
                $"Dear {employeeName}, your leave request has been submitted and is pending approval. " +
                "- Kingdom Preparatory School HR";
            return SendSmsAsync(recipient, message, SmsSenderIds.EmployeeAdmission);
        }

        public static Task<(bool Success, string Message)> SendLeaveDecisionAsync(
            string recipient, string employeeName, string status, DateTime startDate, DateTime endDate)
        {
            string message =
                $"Dear {employeeName}, your leave ({startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}) " +
                $"has been {status.ToUpperInvariant()}. - Kingdom Preparatory School HR";
            return SendSmsAsync(recipient, message, SmsSenderIds.EmployeeAdmission);
        }

        public static Task<(bool Success, string Message)> SendLeaveHrAlertAsync(
            string hrPhone, string employeeName, DateTime startDate, DateTime endDate)
        {
            string message =
                $"Leave request from {employeeName} ({startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}) " +
                "is pending review. - Kingdom Preparatory School";
            return SendSmsAsync(hrPhone, message, SmsSenderIds.EmployeeAdmission);
        }

        private static void Log(string eventType, string recipient, string senderId, string details)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(Path.Combine(LogDir, "sms.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{eventType}] From: {senderId} To: {recipient} | {details}\n");
            }
            catch { }
        }
    }
}

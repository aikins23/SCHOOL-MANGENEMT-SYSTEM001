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
            // Real sending only when enabled AND provider is Arkesel AND a key exists.
            if (AppConfig.Sms.Enabled &&
                string.Equals(AppConfig.Sms.Provider, "Arkesel", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(AppConfig.Sms.ApiKey))
            {
                return new ArkeselSmsProvider(AppConfig.Sms.ApiKey);
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

        public static Task<(bool Success, string Message)> SendStudentAdmissionAsync(string recipient, string fullName)
        {
            string message =
$@"Welcome to Kingdom Preparatory School, {fullName}!

You have been successfully registered as a student.

- Administration";
            return SendSmsAsync(recipient, message, SmsSenderIds.StudentAdmission);
        }

        public static Task<(bool Success, string Message)> SendEmployeeAdmissionAsync(string recipient, string fullName)
        {
            string message =
$@"Welcome to Kingdom Preparatory School, {fullName}!

You have been successfully registered as an employee.

- Administration";
            return SendSmsAsync(recipient, message, SmsSenderIds.EmployeeAdmission);
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

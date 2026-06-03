using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for sending school notifications (Email)
    /// Supports fee reminders, exam alerts, leave approvals, and general announcements.
    /// </summary>
    public static class NotificationService
    {
        public enum NotificationType
        {
            FeeReminder,
            ExamResult,
            LeaveApproval,
            PaymentReceived,
            GeneralAnnouncement
        }

        /// <summary>
        /// Sends an email notification.
        /// </summary>
        public static async Task<(bool Success, string Message)> SendEmailAsync(
            string recipient, string subject, string body, NotificationType type)
        {
            try
            {
                // Check if email is configured
                if (!AppConfig.Email.IsConfigured)
                {
                    LogEmailEvent("NOT_CONFIGURED", recipient, subject, "Email service not configured");
                    return (false, "Email service is not configured. Please set up email settings first.");
                }

                // Validate recipient
                if (string.IsNullOrWhiteSpace(recipient))
                    return (false, "Recipient email address is required.");

                // Create SMTP client
                using (var smtpClient = new SmtpClient(AppConfig.Email.SmtpServer, AppConfig.Email.SmtpPort))
                {
                    smtpClient.EnableSsl = AppConfig.Email.UseSSL;
                    smtpClient.Credentials = new NetworkCredential(
                        AppConfig.Email.SmtpUsername,
                        AppConfig.Email.SmtpPassword
                    );
                    smtpClient.Timeout = 30000; // 30 second timeout

                    // Create mail message
                    using (var mailMessage = new MailMessage(
                        new MailAddress(AppConfig.Email.FromEmail, AppConfig.Email.FromName),
                        new MailAddress(recipient)
                    ))
                    {
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = false;

                        // Send email asynchronously
                        await smtpClient.SendMailAsync(mailMessage);

                        // Log successful send
                        LogEmailEvent("SUCCESS", recipient, subject, $"Sent successfully ({type})");

                        return (true, $"Email sent to {recipient}");
                    }
                }
            }
            catch (SmtpException ex)
            {
                LogEmailEvent("SMTP_ERROR", recipient, subject, $"SMTP Error: {ex.Message}");
                return (false, $"Email service error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogEmailEvent("ERROR", recipient, subject, $"Error: {ex.Message}");
                return (false, $"Failed to send email: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a fee payment reminder to guardian.
        /// </summary>
        public static async Task<(bool Success, string Message)> SendFeeReminderAsync(
            string studentName, string guardianEmail, decimal balance, string studentClass = "")
        {
            if (string.IsNullOrWhiteSpace(guardianEmail) || balance <= 0)
                return (false, "Invalid email or balance");

            string subject = "Fee Payment Reminder - Kingdom Preparatory School";
            string body = $@"Dear Guardian,

This is a friendly reminder that {studentName}{(string.IsNullOrWhiteSpace(studentClass) ? "" : $" ({studentClass})")} has an outstanding balance of GHS {balance:N2}.

Please ensure payment is made at the earliest convenience to avoid any disruptions to your child's education.

For payment inquiries, please contact the accounts office.

Thank you for your cooperation.

Best regards,
Kingdom Preparatory School
Accounts Department";

            return await SendEmailAsync(guardianEmail, subject, body, NotificationType.FeeReminder);
        }

        /// <summary>
        /// Sends exam results notification to student.
        /// </summary>
        public static async Task<(bool Success, string Message)> SendExamResultAlertAsync(
            string studentName, string studentEmail, string term, string year, string className = "")
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return (false, "Student email is required");

            string subject = $"Academic Results Published - {term} {year}";
            string body = $@"Hello {studentName},

We are pleased to inform you that your academic results for {term} {year}{(string.IsNullOrWhiteSpace(className) ? "" : $" ({className})")} have been published.

You can view your detailed results and report card through:
1. The school portal (if available)
2. Requesting a printed report card from the administration office

If you have any questions regarding your results, please speak with your form tutor or visit the Academic Office.

Best regards,
Academic Office
Kingdom Preparatory School";

            return await SendEmailAsync(studentEmail, subject, body, NotificationType.ExamResult);
        }

        /// <summary>
        /// Sends leave approval/rejection notification to employee.
        /// </summary>
        public static async Task<(bool Success, string Message)> SendLeaveApprovalAsync(
            string employeeName, string employeeEmail, string status,
            DateTime startDate, DateTime endDate, string leaveType = "")
        {
            if (string.IsNullOrWhiteSpace(employeeEmail))
                return (false, "Employee email is required");

            int duration = (endDate.Date - startDate.Date).Days + 1;
            string statusText = status.ToUpper();

            string subject = $"Leave Request {statusText} - Kingdom Preparatory School";
            string body = $@"Dear {employeeName},

Your leave application has been {statusText.ToLower()}.

Leave Details:
  Type: {leaveType}
  Start Date: {startDate:MMMM dd, yyyy}
  End Date: {endDate:MMMM dd, yyyy}
  Duration: {duration} day(s)
  Status: {statusText}

If you have any questions or concerns regarding this decision, please contact the Human Resources office.

Best regards,
Human Resources Department
Kingdom Preparatory School";

            return await SendEmailAsync(employeeEmail, subject, body, NotificationType.LeaveApproval);
        }

        /// <summary>Confirms to the employee that their leave request was received.</summary>
        public static async Task<(bool Success, string Message)> SendLeaveSubmittedAsync(
            string employeeName, string employeeEmail, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(employeeEmail))
                return (false, "Employee email is required");

            int duration = (endDate.Date - startDate.Date).Days + 1;
            string subject = "Leave Request Received - Kingdom Preparatory School";
            string body = $@"Dear {employeeName},

Your leave application has been received and is pending approval.

  Start Date: {startDate:MMMM dd, yyyy}
  End Date: {endDate:MMMM dd, yyyy}
  Duration: {duration} day(s)
  Status: PENDING

You will be notified once a decision has been made.

Best regards,
Human Resources Department
Kingdom Preparatory School";

            return await SendEmailAsync(employeeEmail, subject, body, NotificationType.LeaveApproval);
        }

        /// <summary>Alerts the HR address that a new leave request needs review.</summary>
        public static async Task<(bool Success, string Message)> SendLeaveRequestHrAlertAsync(
            string hrEmail, string employeeName, DateTime startDate, DateTime endDate, string reason)
        {
            if (string.IsNullOrWhiteSpace(hrEmail))
                return (false, "HR email is required");

            int duration = (endDate.Date - startDate.Date).Days + 1;
            string subject = $"Leave Request Pending Review - {employeeName}";
            string body = $@"A new leave request requires review:

  Employee: {employeeName}
  Start Date: {startDate:MMMM dd, yyyy}
  End Date: {endDate:MMMM dd, yyyy}
  Duration: {duration} day(s)
  Reason: {reason}

Please review it in the Leave Approval screen.

Kingdom Preparatory School";

            return await SendEmailAsync(hrEmail, subject, body, NotificationType.LeaveApproval);
        }

        /// <summary>
        /// Sends payment received confirmation to guardian.
        /// </summary>
        public static async Task<(bool Success, string Message)> SendPaymentReceivedAsync(
            string studentName, string guardianEmail, decimal amountPaid, decimal newBalance,
            DateTime paymentDate, string studentClass = "")
        {
            if (string.IsNullOrWhiteSpace(guardianEmail))
                return (false, "Guardian email is required");

            string balanceText = newBalance > 0
                ? $"Remaining Balance: GHS {newBalance:N2}"
                : "Status: Balance Cleared ✓";

            string subject = "Payment Received - Kingdom Preparatory School";
            string body = $@"Dear Guardian,

We confirm receipt of your payment for {studentName}{(string.IsNullOrWhiteSpace(studentClass) ? "" : $" ({studentClass})")}.

Payment Details:
  Amount Paid: GHS {amountPaid:N2}
  Payment Date: {paymentDate:MMMM dd, yyyy}
  {balanceText}

Thank you for your prompt payment. Your support helps us provide quality education for our students.

For any payment inquiries, please contact the Accounts Office.

Best regards,
Accounts Department
Kingdom Preparatory School";

            return await SendEmailAsync(guardianEmail, subject, body, NotificationType.PaymentReceived);
        }

        /// <summary>
        /// Sends a general announcement email.
        /// </summary>
        public static async Task<(bool Success, string Message)> SendAnnouncementAsync(
            string recipient, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                return (false, "Recipient email is required");

            return await SendEmailAsync(recipient, subject, body, NotificationType.GeneralAnnouncement);
        }

        /// <summary>
        /// Tests email configuration by sending a test email.
        /// </summary>
        public static async Task<(bool Success, string Message)> TestEmailConfigurationAsync(string testEmail)
        {
            if (string.IsNullOrWhiteSpace(testEmail))
                return (false, "Test email address is required");

            string subject = "Test Email - Kingdom Preparatory School";
            string body = $@"This is a test email to verify your email configuration.

If you received this email, your email settings are working correctly.

Sent: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

---
Kingdom Preparatory School";

            return await SendEmailAsync(testEmail, subject, body, NotificationType.GeneralAnnouncement);
        }

        // ─── Logging ───────────────────────────────────────────────────

        private static void LogEmailEvent(string eventType, string recipient, string subject, string details)
        {
            try
            {
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsDir);

                string logPath = Path.Combine(logsDir, "emails.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{eventType}] " +
                                  $"To: {recipient} | Subject: {subject} | {details}\n";

                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for sending school notifications (Email/SMS simulation)
    /// </summary>
    public static class NotificationService
    {
        public enum NotificationType { FeeReminder, ExamResult, LeaveApproval, GeneralAnnouncement }

        public static async Task<(bool Success, string Message)> SendNotificationAsync(string recipient, string subject, string body, NotificationType type)
        {
            try
            {
                // In a production environment, this would use an actual SMTP client or SMS gateway
                // For this project, we will simulate the send process and log it.
                
                await Task.Delay(1000); // Simulate network latency

                // Log the notification (Simulation)
                string logEntry = $"[{DateTime.Now}] NOTIFICATION SENT to {recipient}\nType: {type}\nSubject: {subject}\nBody: {body}\n------------------\n";
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notifications_log.txt");
                System.IO.File.AppendAllText(logPath, logEntry);

                return (true, $"Notification sent successfully to {recipient} via {type}.");
            }
            catch (Exception ex)
            {
                return (false, "Failed to send notification: " + ex.Message);
            }
        }

        public static async Task SendFeeReminderAsync(string studentName, string guardianEmail, decimal balance)
        {
            if (string.IsNullOrWhiteSpace(guardianEmail) || balance <= 0) return;

            string subject = "Fee Payment Reminder - Kingdom Preparatory School";
            string body = $@"Dear Guardian,

This is a friendly reminder that {studentName} has an outstanding balance of GHS {balance:N2}.
Please ensure payment is made at the earliest convenience.

Thank you for your cooperation.";

            await SendNotificationAsync(guardianEmail, subject, body, NotificationType.FeeReminder);
        }

        public static async Task SendExamAlertAsync(string studentName, string studentEmail, string term, string year)
        {
            if (string.IsNullOrWhiteSpace(studentEmail)) return;

            string subject = $"Results Published: {term} {year}";
            string body = $@"Hello {studentName},

Your academic results for {term} {year} have been published. 
You can view them on the school portal or collect a printed report card from the administration.

Best regards,
Academic Office";

            await SendNotificationAsync(studentEmail, subject, body, NotificationType.ExamResult);
        }
    }
}

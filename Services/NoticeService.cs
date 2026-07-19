using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class NoticeService
    {
        private readonly INoticeRepository _repository;

        public NoticeService(INoticeRepository repository)
        {
            _repository = repository;
        }

        public async Task<DataTable> GetHistoryTableAsync()
        {
            return await _repository.GetAsTableAsync();
        }

        public async Task<(bool Success, string Message)> SendNoticeAsync(Notice notice)
        {
            if (notice == null) return (false, "Notice is required.");
            if (string.IsNullOrWhiteSpace(notice.Title)) return (false, "Notice title is required.");
            if (string.IsNullOrWhiteSpace(notice.Message)) return (false, "Notice message is required.");

            bool saved = await _repository.AddAsync(notice);
            if (!saved) return (false, "Failed to save notice.");

            var recipients = await _repository.GetRecipientTableAsync(notice.Target, notice.TargetClass);
            bool sendSms = WantsSms(notice.Channel);
            bool sendEmail = WantsEmail(notice.Channel);
            int attemptedRecipients = 0;
            int smsOk = 0;
            int smsFailed = 0;
            int emailOk = 0;
            int emailFailed = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in recipients.Rows)
            {
                string name = row["DisplayName"]?.ToString() ?? "";
                string phone = row["Phone"]?.ToString() ?? "";
                string email = row["Email"]?.ToString() ?? "";
                string key = (phone + "|" + email).Trim('|');
                if (string.IsNullOrWhiteSpace(key) || !seen.Add(key)) continue;

                attemptedRecipients++;
                string body = BuildBody(notice, name);

                if (sendSms && !string.IsNullOrWhiteSpace(phone))
                {
                    var sms = await SmsService.SendSmsAsync(phone, body, SmsSenderIds.Notice);
                    if (sms.Success) smsOk++; else smsFailed++;
                }

                if (sendEmail && !string.IsNullOrWhiteSpace(email))
                {
                    var mail = await NotificationService.SendAnnouncementAsync(email, notice.Title, body);
                    if (mail.Success) emailOk++; else emailFailed++;
                }
            }

            string status = BuildStatus(sendSms, sendEmail, attemptedRecipients, smsOk, smsFailed, emailOk, emailFailed);
            await _repository.UpdateDeliveryStatusAsync(notice.NoticeID, attemptedRecipients, status);
            return (true, status);
        }

        private static bool WantsSms(string channel)
        {
            channel = channel ?? "";
            return channel.StartsWith("Both", StringComparison.OrdinalIgnoreCase) ||
                   channel.StartsWith("SMS", StringComparison.OrdinalIgnoreCase);
        }

        private static bool WantsEmail(string channel)
        {
            channel = channel ?? "";
            return channel.StartsWith("Both", StringComparison.OrdinalIgnoreCase) ||
                   channel.StartsWith("Email", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildBody(Notice notice, string recipientName)
        {
            var greeting = string.IsNullOrWhiteSpace(recipientName) ? "Dear recipient," : $"Dear {recipientName},";
            return greeting + Environment.NewLine + Environment.NewLine +
                   notice.Message.Trim() + Environment.NewLine + Environment.NewLine +
                   kingdom_Preparatory_School_Management_System.Common.SchoolProfile.DisplayName;
        }

        private static string BuildStatus(bool sms, bool email, int recipients, int smsOk, int smsFailed, int emailOk, int emailFailed)
        {
            if (recipients == 0) return "Saved - no matching recipients";

            var parts = new List<string> { $"Sent to {recipients} recipient(s)" };
            if (sms) parts.Add($"SMS {smsOk} ok/{smsFailed} failed");
            if (email) parts.Add($"Email {emailOk} ok/{emailFailed} failed");
            return string.Join("; ", parts);
        }
    }
}

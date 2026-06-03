using System;
using System.IO;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>Writes the SMS to logs/sms.log instead of sending. Default/offline fallback.</summary>
    public sealed class LogSmsProvider : ISmsProvider
    {
        public string Name => "LogOnly";

        public Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                string snippet = message?.Substring(0, Math.Min(message?.Length ?? 0, 80));
                File.AppendAllText(Path.Combine(dir, "sms.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [LOG_ONLY] From: {senderId} To: {recipient233} | Msg: {snippet}...\n");
            }
            catch { }
            return Task.FromResult((true, $"SMS logged (sender {senderId}) for {recipient233}"));
        }
    }
}

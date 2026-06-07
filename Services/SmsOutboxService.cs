using System;
using System.Threading;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Drains the local SmsOutbox: sends Pending messages via the provider and marks them Sent, or
    /// records a failed attempt (moving to Failed after the attempts cap). Best-effort and re-entrant
    /// safe so overlapping timer ticks never double-send. Runs on dashboard load + the hourly timer.
    /// </summary>
    public static class SmsOutboxService
    {
        private static int _flushing; // 0 = idle, 1 = running

        public static async Task<int> FlushPendingAsync(int maxAttempts = 5, int batch = 50)
        {
            if (Interlocked.Exchange(ref _flushing, 1) == 1) return 0; // already flushing
            int sent = 0;
            try
            {
                var repo = new Data.SmsOutboxRepository(AppConfig.ConnectionString);
                var pending = await repo.GetPendingAsync(maxAttempts, batch);
                foreach (var item in pending)
                {
                    try
                    {
                        var res = await SmsService.SendDirectAsync(item.Recipient, item.Message, item.SenderId);
                        if (res.Success) { await repo.MarkSentAsync(item.Id); sent++; }
                        else await repo.MarkAttemptFailedAsync(item.Id, res.Message, maxAttempts);
                    }
                    catch (Exception exInner)
                    {
                        try { await repo.MarkAttemptFailedAsync(item.Id, exInner.Message, maxAttempts); } catch { }
                    }
                }
                if (sent > 0) LoggerHelper.LogInfo($"SmsOutbox: delivered {sent} queued message(s).");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("SmsOutbox flush: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _flushing, 0);
            }
            return sent;
        }
    }
}

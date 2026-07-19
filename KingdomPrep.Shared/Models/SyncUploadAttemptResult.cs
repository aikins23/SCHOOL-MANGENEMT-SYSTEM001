namespace KingdomPrep.Shared.Models
{
    public class SyncUploadAttemptResult
    {
        public bool Ok { get; set; }
        public int SentCount { get; set; }
        public int ConflictCount { get; set; }
        public int RejectedCount { get; set; }
        public string Message { get; set; }

        public static SyncUploadAttemptResult Success(int sentCount) =>
            new SyncUploadAttemptResult { Ok = true, SentCount = sentCount, Message = "Sync upload completed." };

        public static SyncUploadAttemptResult Skipped(string message) =>
            new SyncUploadAttemptResult { Ok = true, Message = message };

        public static SyncUploadAttemptResult Failed(string message) =>
            new SyncUploadAttemptResult { Ok = false, Message = message };
    }
}

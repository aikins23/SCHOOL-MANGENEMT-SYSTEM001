namespace KingdomPrep.Shared.Models
{
    /// <summary>A queued SMS awaiting delivery (the fields the flusher needs).</summary>
    public class SmsOutboxItem
    {
        public int Id { get; set; }
        public string Recipient { get; set; } = "";
        public string SenderId { get; set; } = "";
        public string Message { get; set; } = "";
        public int Attempts { get; set; }
    }
}

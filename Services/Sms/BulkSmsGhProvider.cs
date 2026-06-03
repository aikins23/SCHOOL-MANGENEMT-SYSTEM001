using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// BulkSMSGh (bulksmsghana.com). GET clientlogin.bulksmsgh.com/smsapi with
    /// query params key, to, msg, sender_id. The response is a plain status code:
    /// "1000" = sent; other codes are errors (see Describe).
    /// </summary>
    public sealed class BulkSmsGhProvider : ISmsProvider
    {
        private const string Endpoint = "https://clientlogin.bulksmsgh.com/smsapi";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string _apiKey;

        public BulkSmsGhProvider(string apiKey) { _apiKey = apiKey ?? ""; }

        public string Name => "BulkSMSGh";

        public async Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (false, "BulkSMSGh API key is not configured.");

            string url = Endpoint
                + "?key=" + Uri.EscapeDataString(_apiKey)
                + "&to=" + Uri.EscapeDataString(recipient233)
                + "&msg=" + Uri.EscapeDataString(message ?? "")
                + "&sender_id=" + Uri.EscapeDataString(senderId);

            try
            {
                using (var resp = await Http.GetAsync(url))
                {
                    string code = (await resp.Content.ReadAsStringAsync() ?? "").Trim();
                    if (code == "1000")
                        return (true, $"SMS sent to {recipient233} (sender {senderId})");
                    return (false, $"BulkSMSGh error {Describe(code)}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"BulkSMSGh request failed: {ex.Message}");
            }
        }

        private static string Describe(string code)
        {
            switch (code)
            {
                case "1002": return "1002: message not sent";
                case "1003": return "1003: insufficient balance";
                case "1004": return "1004: invalid API key";
                case "1005": return "1005: invalid phone number";
                case "1006": return "1006: invalid sender ID (not registered/approved)";
                case "1008": return "1008: empty message";
                default: return string.IsNullOrEmpty(code) ? "(empty response)" : code;
            }
        }
    }
}

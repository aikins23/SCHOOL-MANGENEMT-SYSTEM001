using KingdomPrep.Shared.Models;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// BulkSMSGh (bulksmsghana.com). GET clientlogin.bulksmsgh.com/smsapi with
    /// query params key, to, msg, sender_id. The current API returns JSON
    /// {"success":bool,"code":int,"message":"..."}; older deployments returned a
    /// bare status code string ("1000" = sent). Both are handled.
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
                    string body = (await resp.Content.ReadAsStringAsync() ?? "").Trim();

                    // Current API: JSON {"success":true,"code":1000,...}.
                    // Legacy API: a bare status code string ("1000" = sent).
                    bool ok = Regex.IsMatch(body, "\"success\"\\s*:\\s*true", RegexOptions.IgnoreCase)
                              || body == "1000";
                    if (ok)
                        return (true, $"SMS sent to {recipient233} (sender {senderId})");

                    string code = Match(body, "\"code\"\\s*:\\s*\"?(\\d+)");
                    string msg = Match(body, "\"message\"\\s*:\\s*\"([^\"]*)\"");
                    if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(msg))
                        return (false, $"BulkSMSGh error {Describe(body)}");   // legacy bare code
                    return (false, $"BulkSMSGh error {(string.IsNullOrEmpty(code) ? "" : code + ": ")}{msg}".Trim());
                }
            }
            catch (Exception ex)
            {
                return (false, $"BulkSMSGh request failed: {ex.Message}");
            }
        }

        private static string Match(string input, string pattern)
        {
            var m = Regex.Match(input ?? "", pattern, RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "";
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

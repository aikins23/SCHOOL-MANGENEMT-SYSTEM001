using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Arkesel SMS v2. POST https://sms.arkesel.com/api/v2/sms/send
    /// Header: api-key: &lt;key&gt;. Body: {"sender","message","recipients":["233..."]}.
    /// Success = HTTP 2xx AND the response body contains "status":"success".
    /// </summary>
    public sealed class ArkeselSmsProvider : ISmsProvider
    {
        private const string Endpoint = "https://sms.arkesel.com/api/v2/sms/send";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string _apiKey;

        public ArkeselSmsProvider(string apiKey) { _apiKey = apiKey ?? ""; }

        public string Name => "Arkesel";

        public async Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (false, "Arkesel API key is not configured.");

            string json = "{\"sender\":\"" + Escape(senderId) + "\"," +
                          "\"message\":\"" + Escape(message) + "\"," +
                          "\"recipients\":[\"" + Escape(recipient233) + "\"]}";

            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, Endpoint))
                {
                    req.Headers.TryAddWithoutValidation("api-key", _apiKey);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var resp = await Http.SendAsync(req))
                    {
                        string body = await resp.Content.ReadAsStringAsync();
                        // Arkesel v2 success: {"status":"success", ...}. Tolerate whitespace
                        // around the colon (e.g. "status": "success").
                        bool ok = resp.IsSuccessStatusCode &&
                                  Regex.IsMatch(body ?? "", "\"status\"\\s*:\\s*\"success\"", RegexOptions.IgnoreCase);
                        return ok
                            ? (true, $"SMS sent to {recipient233} (sender {senderId})")
                            : (false, $"Arkesel error ({(int)resp.StatusCode}): {Trim(body)}");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Arkesel request failed: {ex.Message}");
            }
        }

        private static string Escape(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", "\\n");

        private static string Trim(string s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > 200 ? s.Substring(0, 200) : s);
    }
}

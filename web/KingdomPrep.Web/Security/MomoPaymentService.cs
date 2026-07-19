using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KingdomPrep.Web.Data;

namespace KingdomPrep.Web.Security;

public sealed record MomoPaymentResult(bool IsSuccess, string? ReferenceId = null, string? ErrorMessage = null);
public sealed record MomoVerificationResult(bool IsValid, string? Status = null, decimal Amount = 0, string? ErrorMessage = null);

public interface IMomoPaymentService
{
    Task<MomoPaymentResult> RequestToPayAsync(string mobileNumber, decimal amount, string externalId, string payerMessage, string payeeNote, Guid? schoolId = null, CancellationToken cancellationToken = default);
    Task<MomoVerificationResult> CheckStatusAsync(string referenceId, Guid? schoolId = null, CancellationToken cancellationToken = default);
}

public sealed class MomoPaymentService(
    HttpClient httpClient,
    IPaymentGatewaySettingsService settingsService,
    ILogger<MomoPaymentService> logger) : IMomoPaymentService
{
    private async Task<string?> GetAccessTokenAsync(string baseUrl, string apiUser, string apiKey, string subscriptionKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/collection/token/");
        var authBytes = Encoding.UTF8.GetBytes($"{apiUser}:{apiKey}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("MTN MoMo token request failed with status {StatusCode}.", response.StatusCode);
                return null;
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            if (json.RootElement.TryGetProperty("access_token", out var tokenProp))
            {
                return tokenProp.GetString();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while fetching MTN MoMo access token.");
        }

        return null;
    }

    private static string FormatPhoneNumber(string phone)
    {
        var cleaned = new string(phone.Where(char.IsDigit).ToArray());
        if (cleaned.StartsWith("0") && cleaned.Length == 10)
        {
            return "233" + cleaned.Substring(1);
        }
        if (cleaned.StartsWith("233") && cleaned.Length == 12)
        {
            return cleaned;
        }
        return cleaned;
    }

    public async Task<MomoPaymentResult> RequestToPayAsync(string mobileNumber, decimal amount, string externalId, string payerMessage, string payeeNote, Guid? schoolId = null, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetRuntimeSettingsAsync(schoolId, cancellationToken);
        var subscriptionKey = settings.MomoSubscriptionKey.Trim();
        var apiUser = settings.MomoApiUser.Trim();
        var apiKey = settings.MomoApiKey.Trim();
        var targetEnv = settings.MomoTargetEnvironment.Trim();
        var baseUrl = settings.MomoBaseUrl.Trim();

        if (string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(apiUser) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new MomoPaymentResult(false, ErrorMessage: "MTN MoMo API credentials are not fully configured for this school.");
        }

        var formattedPhone = FormatPhoneNumber(mobileNumber);
        if (string.IsNullOrWhiteSpace(formattedPhone) || (formattedPhone.Length != 12 && targetEnv != "sandbox"))
        {
            return new MomoPaymentResult(false, ErrorMessage: "Invalid mobile number format. Please enter a valid 10-digit number (e.g., 024XXXXXXX).");
        }

        var token = await GetAccessTokenAsync(baseUrl, apiUser, apiKey, subscriptionKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MomoPaymentResult(false, ErrorMessage: "Could not authenticate with MTN MoMo API.");
        }

        var referenceId = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/collection/v1_0/requesttopay");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Reference-Id", referenceId);
        request.Headers.Add("X-Target-Environment", targetEnv);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var payload = new
        {
            amount = amount.ToString("0.00", CultureInfo.InvariantCulture),
            currency = targetEnv.Equals("sandbox", StringComparison.OrdinalIgnoreCase) ? "EUR" : "GHS",
            externalId = externalId,
            payer = new
            {
                partyIdType = "MSISDN",
                partyId = formattedPhone
            },
            payerMessage = string.IsNullOrWhiteSpace(payerMessage) ? "School Fee Payment" : payerMessage,
            payeeNote = string.IsNullOrWhiteSpace(payeeNote) ? "Fee Payment" : payeeNote
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.IsSuccessStatusCode)
            {
                return new MomoPaymentResult(true, ReferenceId: referenceId);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("MTN MoMo RequestToPay failed. Status: {StatusCode}, Response: {Error}", response.StatusCode, errorContent);
            return new MomoPaymentResult(false, ErrorMessage: $"MoMo initiation failed (Status {response.StatusCode}). Check account or number.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during MTN MoMo RequestToPay.");
            return new MomoPaymentResult(false, ErrorMessage: "Network or server error while initiating MoMo payment.");
        }
    }

    public async Task<MomoVerificationResult> CheckStatusAsync(string referenceId, Guid? schoolId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
        {
            return new MomoVerificationResult(false, ErrorMessage: "Reference ID is missing.");
        }

        var settings = await settingsService.GetRuntimeSettingsAsync(schoolId, cancellationToken);
        var subscriptionKey = settings.MomoSubscriptionKey.Trim();
        var apiUser = settings.MomoApiUser.Trim();
        var apiKey = settings.MomoApiKey.Trim();
        var targetEnv = settings.MomoTargetEnvironment.Trim();
        var baseUrl = settings.MomoBaseUrl.Trim();

        if (string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(apiUser) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new MomoVerificationResult(false, ErrorMessage: "MTN MoMo API credentials are not configured for this school.");
        }

        var token = await GetAccessTokenAsync(baseUrl, apiUser, apiKey, subscriptionKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MomoVerificationResult(false, ErrorMessage: "Authentication with MTN MoMo API failed.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/collection/v1_0/requesttopay/{Uri.EscapeDataString(referenceId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Target-Environment", targetEnv);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("MTN MoMo status check failed with status {StatusCode} for reference {ReferenceId}.", response.StatusCode, referenceId);
                return new MomoVerificationResult(false, ErrorMessage: "Could not fetch transaction status from MTN MoMo.");
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = json.RootElement;

            var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
            var amountStr = root.TryGetProperty("amount", out var amountProp) ? amountProp.GetString() : "0";
            decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);

            if (string.Equals(status, "SUCCESSFUL", StringComparison.OrdinalIgnoreCase))
            {
                return new MomoVerificationResult(true, Status: status, Amount: amount);
            }
            else if (string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                return new MomoVerificationResult(false, Status: status, ErrorMessage: "Payment is still PENDING approval on the phone.");
            }
            else
            {
                var reason = root.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "Transaction failed or rejected.";
                return new MomoVerificationResult(false, Status: status, ErrorMessage: $"Payment failed: {status} ({reason}).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception checking status for MoMo reference {ReferenceId}.", referenceId);
            return new MomoVerificationResult(false, ErrorMessage: "Error verifying transaction status.");
        }
    }
}

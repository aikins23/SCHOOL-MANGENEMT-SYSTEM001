using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using KingdomPrep.Web.Data;

namespace KingdomPrep.Web.Security;

public sealed record PaystackVerificationResult(bool IsValid, string? ErrorMessage = null);

public interface IPaystackPaymentVerifier
{
    Task<PaystackVerificationResult> VerifyAsync(string reference, decimal expectedAmount, Guid? schoolId = null, CancellationToken cancellationToken = default);
}

public sealed class PaystackPaymentVerifier(
    HttpClient httpClient,
    IPaymentGatewaySettingsService settingsService,
    ILogger<PaystackPaymentVerifier> logger) : IPaystackPaymentVerifier
{
    public async Task<PaystackVerificationResult> VerifyAsync(string reference, decimal expectedAmount, Guid? schoolId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return new PaystackVerificationResult(false, "Payment reference is missing.");
        }

        if (expectedAmount <= 0)
        {
            return new PaystackVerificationResult(false, "Payment amount is invalid.");
        }

        var settings = await settingsService.GetRuntimeSettingsAsync(schoolId, cancellationToken);
        var secretKey = settings.PaystackSecretKey.Trim();
        if (string.IsNullOrWhiteSpace(secretKey) || !secretKey.StartsWith("sk_", StringComparison.Ordinal))
        {
            return new PaystackVerificationResult(false, "Paystack secret key is not configured for this school.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.paystack.co/transaction/verify/{Uri.EscapeDataString(reference)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Paystack verification failed with status {StatusCode} for reference {Reference}.", response.StatusCode, reference);
                return new PaystackVerificationResult(false, "Payment could not be verified.");
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = json.RootElement;

            if (!root.TryGetProperty("status", out var rootStatus) || !rootStatus.GetBoolean())
            {
                return new PaystackVerificationResult(false, "Payment was not accepted by Paystack.");
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return new PaystackVerificationResult(false, "Payment verification response is incomplete.");
            }

            var transactionStatus = data.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
            var verifiedReference = data.TryGetProperty("reference", out var referenceElement) ? referenceElement.GetString() : null;
            var verifiedAmount = data.TryGetProperty("amount", out var amountElement) ? amountElement.GetInt64() : -1;
            var expectedMinorUnits = (long)decimal.Round(expectedAmount * 100m, 0, MidpointRounding.AwayFromZero);

            if (!string.Equals(transactionStatus, "success", StringComparison.OrdinalIgnoreCase))
            {
                return new PaystackVerificationResult(false, "Payment was not successful.");
            }

            if (!string.Equals(verifiedReference, reference, StringComparison.Ordinal))
            {
                return new PaystackVerificationResult(false, "Payment reference does not match.");
            }

            if (verifiedAmount != expectedMinorUnits)
            {
                return new PaystackVerificationResult(false, string.Format(CultureInfo.InvariantCulture, "Payment amount mismatch. Expected {0}, received {1}.", expectedMinorUnits, verifiedAmount));
            }

            return new PaystackVerificationResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Paystack verification failed for reference {Reference}.", reference);
            return new PaystackVerificationResult(false, "Payment verification failed.");
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Api;

public static class PaystackWebhookEndpoints
{
    public static void MapPaystackWebhooks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/paystack", async (
            HttpRequest request,
            IPaymentGatewaySettingsService settingsService,
            IOnlinePaymentIntentRepository intentRepository,
            IDbContextFactory<AppDbContext> dbFactory,
            IFinancePortalRepository financeRepo,
            ILogger<Program> logger) =>
        {
            // Paystack requires reading the raw body to compute the HMAC signature
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            // Verify Signature
            var signatureHeader = request.Headers["x-paystack-signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                logger.LogWarning("Paystack webhook missing signature header.");
                return Results.BadRequest("Missing signature");
            }

            var secretCandidates = await settingsService.GetPaystackWebhookSecretCandidatesAsync(request.HttpContext.RequestAborted);
            var matchedSecret = MatchWebhookSecret(secretCandidates, body, signatureHeader);
            if (matchedSecret == null)
            {
                logger.LogWarning("Paystack webhook signature verification failed.");
                return Results.BadRequest("Invalid signature");
            }

            // Parse payload
            try
            {
                var payload = JsonSerializer.Deserialize<PaystackWebhookPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload?.Event == "charge.success" && payload.Data != null)
                {
                    string reference = payload.Data.Reference;
                    decimal amountPaid = payload.Data.Amount / 100m; // Paystack sends amounts in pesewas/kobo

                    logger.LogInformation("Paystack webhook processing successful charge for reference {Reference}", reference);

                    // Parse the student ID from reference. Format is typically FEE-{StudentID}-{Ticks}
                    var refParts = reference.Split('-');
                    if (refParts.Length >= 2 && int.TryParse(refParts[1], out int studentId))
                    {
                        var intent = await intentRepository.GetByReferenceAsync(reference, request.HttpContext.RequestAborted);
                        if (intent != null && matchedSecret.SchoolId.HasValue && intent.SchoolId != matchedSecret.SchoolId)
                        {
                            logger.LogWarning("Paystack webhook reference {Reference} matched a different school secret than its payment intent.", reference);
                            return Results.BadRequest("Invalid reference");
                        }

                        if (intent != null && Math.Abs(intent.Amount - amountPaid) > 0.01m)
                        {
                            logger.LogWarning("Paystack webhook amount mismatch for reference {Reference}. Expected {Expected}, received {Received}.", reference, intent.Amount, amountPaid);
                            return Results.BadRequest("Invalid amount");
                        }

                        var schoolId = intent?.SchoolId ?? matchedSecret.SchoolId;
                        var effectiveStudentId = intent?.StudentID ?? studentId;
                        var effectiveAmount = intent?.Amount ?? amountPaid;
                        var effectiveBalanceBefore = intent?.BalanceBeforePayment;

                        await using var db = await dbFactory.CreateDbContextAsync(request.HttpContext.RequestAborted);
                        var studentQuery = db.Students.AsNoTracking().Where(s => s.StudentID == effectiveStudentId);
                        if (schoolId.HasValue) studentQuery = studentQuery.Where(s => s.SchoolId == schoolId.Value);
                        var student = await studentQuery.FirstOrDefaultAsync(request.HttpContext.RequestAborted);
                        var currentBalance = await financeRepo.GetCurrentBalanceAsync(effectiveStudentId, schoolId);
                        var balanceBefore = effectiveBalanceBefore ?? currentBalance;

                        var paymentRecord = new PaymentRecordEntity
                        {
                            StudentID = effectiveStudentId,
                            ClassID = intent?.ClassID ?? student?.ClassID,
                            StudentName = intent?.StudentName ?? student?.FullName,
                            AmountPaid = effectiveAmount,
                            Balance = Math.Max(0, balanceBefore - effectiveAmount),
                            PaymentDate = DateTime.Now,
                            SchoolId = schoolId,
                            PaymentReference = reference
                        };

                        await financeRepo.RecordFeeCollectionAsync(paymentRecord, paymentRecord.Balance, schoolId, "system:webhook");
                        await intentRepository.MarkCompletedAsync(reference, request.HttpContext.RequestAborted);
                        logger.LogInformation("Webhook payment recorded for Student {StudentID}, Amount: {Amount}", effectiveStudentId, effectiveAmount);
                    }
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Paystack webhook.");
                return Results.StatusCode(500);
            }
        }).ExcludeFromDescription().DisableAntiforgery(); // Webhooks must not require antiforgery tokens
    }

    private static PaystackWebhookSecretCandidate? MatchWebhookSecret(
        IReadOnlyList<PaystackWebhookSecretCandidate> candidates,
        string body,
        string signatureHeader)
    {
        foreach (var candidate in candidates)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(candidate.SecretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedSignature),
                    Encoding.UTF8.GetBytes(signatureHeader.ToLowerInvariant())))
            {
                return candidate;
            }
        }

        return null;
    }
}

public class PaystackWebhookPayload
{
    public string Event { get; set; } = "";
    public PaystackWebhookData? Data { get; set; }
}

public class PaystackWebhookData
{
    public string Reference { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}

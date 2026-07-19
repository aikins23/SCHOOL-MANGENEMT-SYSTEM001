namespace KingdomPrep.Web.Api;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync")
            .DisableAntiforgery()
            .WithTags("Sync");

        group.MapGet("/status", async (
            HttpContext http,
            IConfiguration config,
            SyncDeviceRegistryService registry,
            CancellationToken cancellationToken) =>
        {
            var supplied = http.Request.Headers["X-Sync-Key"].FirstOrDefault();
            var schoolId = TryHeaderGuid(http, "X-School-Id");
            var deviceId = TryHeaderGuid(http, "X-Device-Id");

            SyncDeviceAuthorizationResult? deviceAuth = null;
            if (!string.IsNullOrWhiteSpace(supplied) || schoolId.HasValue || deviceId.HasValue)
            {
                deviceAuth = await registry.AuthorizeAsync(
                    schoolId ?? Guid.Empty,
                    deviceId ?? Guid.Empty,
                    supplied,
                    cancellationToken);
                if (!deviceAuth.Ok) return AuthFailure(deviceAuth);
            }

            return Results.Ok(new SyncStatusResponse
            {
                Enabled = deviceAuth?.Ok == true && IsConfigured(config),
                DeviceAuthorized = deviceAuth?.Ok ?? false,
                AuthMode = deviceAuth?.Mode ?? "",
                Message = deviceAuth?.Ok == true ? "Device is authorized for sync." : "Sync endpoint is reachable.",
                ServerTimeUtc = DateTime.UtcNow,
                AllowedTables = deviceAuth?.Ok == true ? SyncTablePolicy.AllowedTables : []
            });
        });

        group.MapPost("/upload", async (
            HttpContext http,
            IConfiguration config,
            SyncDeviceRegistryService registry,
            SyncInboxService sync,
            SyncUploadRequest request,
            CancellationToken cancellationToken) =>
        {
            var auth = await AuthorizeDeviceAsync(http, registry, request.SchoolId, request.DeviceId, cancellationToken);
            if (auth is not null) return auth;

            var headerSchoolId = http.Request.Headers["X-School-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerSchoolId)
                && Guid.TryParse(headerSchoolId, out var headerSchool)
                && headerSchool != request.SchoolId)
            {
                return Results.BadRequest(new { error = "X-School-Id does not match request SchoolId." });
            }

            var headerDeviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerDeviceId)
                && Guid.TryParse(headerDeviceId, out var headerDevice)
                && headerDevice != request.DeviceId)
            {
                return Results.BadRequest(new { error = "X-Device-Id does not match request DeviceId." });
            }

            var result = await sync.AcceptUploadAsync(request, cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/pull", async (
            HttpContext http,
            IConfiguration config,
            SyncDeviceRegistryService registry,
            SyncInboxService sync,
            SyncPullRequest request,
            CancellationToken cancellationToken) =>
        {
            var auth = await AuthorizeDeviceAsync(http, registry, request.SchoolId, request.DeviceId, cancellationToken);
            if (auth is not null) return auth;

            if (request.SchoolId == Guid.Empty || request.DeviceId == Guid.Empty)
                return Results.BadRequest(new { error = "SchoolId and DeviceId are required." });

            if (!SyncTablePolicy.IsAllowed(request.TableName))
                return Results.BadRequest(new { error = "Table is not allowed for sync." });

            var result = await sync.PullAsync(request, cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/register-device", async (
            HttpContext http,
            IConfiguration config,
            SyncDeviceRegistryService registry,
            SyncDeviceRegistrationRequest request,
            CancellationToken cancellationToken) =>
        {
            var provisioningKey = config["Sync:ProvisioningKey"];
            if (string.IsNullOrWhiteSpace(provisioningKey))
            {
                return Results.Problem(
                    "Sync provisioning is not configured on this server.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var supplied = http.Request.Headers["X-Provisioning-Key"].FirstOrDefault();
            if (!FixedTimeEquals(provisioningKey, supplied))
                return Results.Unauthorized();

            var result = await registry.RegisterAsync(request, cancellationToken);
            return result.Ok ? Results.Ok(result) : Results.BadRequest(result);
        });

        return app;
    }

    private static async Task<IResult?> AuthorizeDeviceAsync(
        HttpContext http,
        SyncDeviceRegistryService registry,
        Guid schoolId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var supplied = http.Request.Headers["X-Sync-Key"].FirstOrDefault();
        var auth = await registry.AuthorizeAsync(schoolId, deviceId, supplied, cancellationToken);
        return auth.Ok ? null : AuthFailure(auth);
    }

    private static bool IsConfigured(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["Sync:ApiKey"]) ||
        !string.IsNullOrWhiteSpace(config["Sync:ProvisioningKey"]);

    private static IResult AuthFailure(SyncDeviceAuthorizationResult auth) =>
        Results.Json(new { error = auth.Message }, statusCode: auth.StatusCode);

    private static Guid? TryHeaderGuid(HttpContext http, string headerName)
    {
        var raw = http.Request.Headers[headerName].FirstOrDefault();
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static bool FixedTimeEquals(string expected, string? supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied)) return false;

        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = System.Text.Encoding.UTF8.GetBytes(supplied);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

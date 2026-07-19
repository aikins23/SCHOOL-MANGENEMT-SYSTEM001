namespace KingdomPrep.Web.Security;

public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "same-origin");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            headers.TryAdd("Content-Security-Policy",
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'self'; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self' data:; " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "connect-src 'self' wss: ws:;");

            await next();
        });
    }

    public static IApplicationBuilder UseSyncRequestSizeLimit(this IApplicationBuilder app, IConfiguration configuration)
    {
        var maxBytes = configuration.GetValue<long?>("Security:SyncUploadMaxBytes") ?? 2L * 1024L * 1024L;
        if (maxBytes < 1) maxBytes = 2L * 1024L * 1024L;

        return app.Use(async (context, next) =>
        {
            if (HttpMethods.IsPost(context.Request.Method)
                && context.Request.Path.StartsWithSegments("/api/sync/upload", StringComparison.OrdinalIgnoreCase)
                && context.Request.ContentLength.HasValue
                && context.Request.ContentLength.Value > maxBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Sync upload payload is too large.\"}");
                return;
            }

            await next();
        });
    }
}

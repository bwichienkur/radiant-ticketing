using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace EnhancementHub.Infrastructure.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _includeContentSecurityPolicy;
    private readonly bool _allowUnsafeEval;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        bool includeContentSecurityPolicy,
        bool allowUnsafeEval = true)
    {
        _next = next;
        _includeContentSecurityPolicy = includeContentSecurityPolicy;
        _allowUnsafeEval = allowUnsafeEval;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["X-XSS-Protection"] = "0";

        if (_includeContentSecurityPolicy && !headers.ContainsKey("Content-Security-Policy"))
        {
            var scriptSrc = _allowUnsafeEval
                ? "script-src 'self' 'unsafe-inline' 'unsafe-eval'; "
                : "script-src 'self' 'unsafe-inline'; ";

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                scriptSrc +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'";
        }

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        bool includeContentSecurityPolicy = true,
        bool allowUnsafeEval = true) =>
        app.UseMiddleware<SecurityHeadersMiddleware>(includeContentSecurityPolicy, allowUnsafeEval);
}

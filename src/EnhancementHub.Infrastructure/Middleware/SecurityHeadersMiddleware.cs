using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace EnhancementHub.Infrastructure.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _includeContentSecurityPolicy;

    public SecurityHeadersMiddleware(RequestDelegate next, bool includeContentSecurityPolicy)
    {
        _next = next;
        _includeContentSecurityPolicy = includeContentSecurityPolicy;
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
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
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
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool includeContentSecurityPolicy = true) =>
        app.UseMiddleware<SecurityHeadersMiddleware>(includeContentSecurityPolicy);
}

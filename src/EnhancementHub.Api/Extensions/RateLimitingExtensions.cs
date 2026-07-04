using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EnhancementHub.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string LoginPolicy = "login";
    public const string UploadPolicy = "upload";
    public const string AiAnalysisPolicy = "ai-analysis";

    public static IServiceCollection AddEnhancementHubRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(LoginPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, "login"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0
                    }));

            options.AddPolicy(UploadPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, "upload"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 20,
                        QueueLimit = 0
                    }));
            options.AddPolicy(AiAnalysisPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, "ai-analysis"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 5,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext httpContext, string policyName)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = httpContext.User?.FindFirst("sub")?.Value
            ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return userId is null ? $"{policyName}:{ip}" : $"{policyName}:{userId}";
    }
}

using EnhancementHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnhancementHub.Infrastructure.DependencyInjection;

public static class HealthCheckServiceExtensions
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddEnhancementHubHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<EnhancementHubDbContext>(
                name: "database",
                tags: [ReadyTag]);

        return services;
    }

    public static WebApplication MapEnhancementHubHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag)
        });

        return app;
    }
}

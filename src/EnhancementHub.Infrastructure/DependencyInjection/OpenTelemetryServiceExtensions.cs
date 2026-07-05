using EnhancementHub.Application.Options;
using EnhancementHub.Infrastructure.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EnhancementHub.Infrastructure.DependencyInjection;

public static class OpenTelemetryServiceExtensions
{
    public static IServiceCollection AddEnhancementHubOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string? serviceNameOverride = null)
    {
        services.Configure<ObservabilityOptions>(
            configuration.GetSection(ObservabilityOptions.SectionName));

        var options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        if (!options.Enabled)
        {
            return services;
        }

        var serviceName = serviceNameOverride ?? options.ServiceName;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                if (options.InstrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }

                if (options.InstrumentHttpClient)
                {
                    tracing.AddHttpClientInstrumentation();
                }

                if (options.InstrumentEntityFramework)
                {
                    tracing.AddEntityFrameworkCoreInstrumentation();
                }

                if (options.InstrumentBackgroundJobs)
                {
                    tracing.AddSource(EnhancementHubTelemetry.ActivitySourceName);
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                if (options.InstrumentAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                if (options.InstrumentHttpClient)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                metrics.AddMeter(EnhancementHubTelemetry.MeterName);

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });

        return services;
    }

    public static WebApplication MapEnhancementHubObservabilityEndpoints(
        this WebApplication app,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        if (options.Enabled && options.EnablePrometheusMetrics)
        {
            app.MapPrometheusScrapingEndpoint();
        }

        return app;
    }

    public static void RegisterHangfireTelemetryFilters(ObservabilityOptions options)
    {
        if (!options.Enabled || !options.InstrumentBackgroundJobs)
        {
            return;
        }

        Hangfire.GlobalJobFilters.Filters.Add(new HangfireTelemetryFilter());
    }
}

namespace EnhancementHub.Application.Options;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool Enabled { get; set; }

    public string ServiceName { get; set; } = "EnhancementHub";

    public string? OtlpEndpoint { get; set; }

    public bool EnablePrometheusMetrics { get; set; } = true;

    public bool InstrumentEntityFramework { get; set; } = true;

    public bool InstrumentHttpClient { get; set; } = true;

    public bool InstrumentAspNetCore { get; set; } = true;

    public bool InstrumentBackgroundJobs { get; set; } = true;
}

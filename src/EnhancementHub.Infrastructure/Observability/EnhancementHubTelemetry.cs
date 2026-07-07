using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EnhancementHub.Infrastructure.Observability;

public static class EnhancementHubTelemetry
{
    public const string ActivitySourceName = "EnhancementHub";
    public const string MeterName = "EnhancementHub";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Histogram<double> JobDurationSeconds = Meter.CreateHistogram<double>(
        "enhancementhub.job.duration.seconds",
        unit: "s",
        description: "Background job execution duration");

    public static readonly Counter<long> JobCompletedTotal = Meter.CreateCounter<long>(
        "enhancementhub.job.completed.total",
        description: "Background jobs completed");

    public static readonly Counter<long> JobFailedTotal = Meter.CreateCounter<long>(
        "enhancementhub.job.failed.total",
        description: "Background jobs failed");

    public static readonly Counter<long> FeatureFlagCacheHits = Meter.CreateCounter<long>(
        "enhancementhub.feature_flag.cache.hits",
        description: "Feature flag cache hits");

    public static readonly Counter<long> FeatureFlagCacheMisses = Meter.CreateCounter<long>(
        "enhancementhub.feature_flag.cache.misses",
        description: "Feature flag cache misses");
}

namespace EnhancementHub.Application.Options;

public sealed class SystemIntelligenceOptions
{
    public const string SectionName = "SystemIntelligence";

    public bool IncrementalGraphEnabled { get; set; } = true;

    public int GraphQueryDefaultPageSize { get; set; } = 200;

    public int GraphQueryMaxPageSize { get; set; } = 500;

    public int GraphQueryDefaultMaxDepth { get; set; } = 4;

    public bool DocumentationCacheEnabled { get; set; } = true;

    public int DocumentationCacheTtlMinutes { get; set; } = 60;

    public bool DiffOnlyDriftEnabled { get; set; } = true;

    public int ScheduledDriftScanIntervalHours { get; set; } = 24;
}

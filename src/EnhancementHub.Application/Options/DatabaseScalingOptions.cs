namespace EnhancementHub.Application.Options;

public sealed class DatabaseScalingOptions
{
    public const string SectionName = "DatabaseScaling";

    public int MaxPoolSize { get; set; } = 100;

    public int MinPoolSize { get; set; } = 0;

    public int ConnectionTimeoutSeconds { get; set; } = 15;

    public int SchemaScanMaxConcurrency { get; set; } = 2;
}

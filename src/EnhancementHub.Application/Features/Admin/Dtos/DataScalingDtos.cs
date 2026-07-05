namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record DataScalingStatusDto(
    DateTime GeneratedAtUtc,
    VectorSearchConfigurationStatusDto VectorSearch,
    DatabaseConnectionScalingStatusDto Database,
    DataArchivalStatusDto Archival);

public sealed record VectorSearchConfigurationStatusDto(
    string Provider,
    bool IsProductionReady,
    string? RecommendedProvider,
    int Dimensions,
    IReadOnlyList<ConfigurationValidationIssueDto> Issues);

public sealed record DatabaseConnectionScalingStatusDto(
    bool ReadReplicaConfigured,
    string PrimaryConnectionName,
    string ReportingConnectionName,
    int MaxPoolSize,
    int SchemaScanMaxConcurrency,
    string DatabaseProvider);

public sealed record DataArchivalStatusDto(
    long AuditLogCount,
    long AiPromptRunCount,
    long EligibleAiPromptRunArchiveCount,
    bool ArchiveBeforeDeleteEnabled,
    string? ArchivePath,
    int AiPromptRunsRetentionDays,
    IReadOnlyList<string> Recommendations);

namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record BackgroundJobsStatusDto(
    string Provider,
    DateTime GeneratedAtUtc,
    BackgroundJobQueueCountsDto QueueCounts,
    IReadOnlyList<BackgroundJobDefinitionStatusDto> Jobs,
    BackgroundJobHangfireStatsDto? Hangfire);

public sealed record BackgroundJobQueueCountsDto(
    int PendingRepositoryIndexing,
    int AwaitingAiAnalysis,
    int QueuedApplicationDiscovery,
    int PendingDatabaseSchemaScans);

public sealed record BackgroundJobDefinitionStatusDto(
    string JobId,
    string Description,
    string Schedule,
    string? LastExecution,
    string? NextExecution);

public sealed record BackgroundJobHangfireStatsDto(
    long Enqueued,
    long Processing,
    long Scheduled,
    long Succeeded,
    long Failed,
    long Deleted);

namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record IndexFreshnessReportDto(
    int SlaHours,
    int TotalRepositories,
    int FreshCount,
    int StaleCount,
    int NeverIndexedCount,
    int InProgressCount,
    int FailedCount,
    double FreshnessPercent,
    bool SlaMet,
    DateTime GeneratedAtUtc,
    IReadOnlyList<StaleRepositoryDto> StaleRepositories);

public sealed record StaleRepositoryDto(
    Guid Id,
    string Name,
    Guid ApplicationId,
    string? ApplicationName,
    DateTime? LastIndexedAt,
    double? HoursSinceIndexed,
    string IndexingStatus);

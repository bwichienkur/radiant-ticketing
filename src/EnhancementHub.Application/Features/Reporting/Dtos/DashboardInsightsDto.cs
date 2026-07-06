namespace EnhancementHub.Application.Features.Reporting.Dtos;

public sealed record DashboardActivityItemDto(
    string EventType,
    string Title,
    string? Subtitle,
    DateTime OccurredAt,
    Guid? EntityId,
    string LinkPath);

public sealed record DashboardDriftFindingSummaryDto(
    Guid Id,
    string Title,
    string Severity,
    string ConnectionName,
    Guid DatabaseConnectionId,
    DateTime DetectedAt,
    string LinkPath);

public sealed record DashboardInsightsDto(
    IReadOnlyList<DashboardActivityItemDto> RecentActivity,
    IReadOnlyList<DailyRequestCountDto> RequestsLast7Days,
    int MyPendingApprovals,
    int MyAwaitingAnalysis,
    int UnresolvedDriftFindings,
    int StaleRepositoryCount,
    IReadOnlyList<DashboardDriftFindingSummaryDto> TopDriftFindings);

public sealed record DailyRequestCountDto(DateOnly Date, int Count);

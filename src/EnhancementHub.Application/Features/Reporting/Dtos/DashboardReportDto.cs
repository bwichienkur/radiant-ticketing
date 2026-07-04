using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Reporting.Dtos;

public sealed record DashboardReportDto(
    IReadOnlyDictionary<EnhancementRequestStatus, int> RequestsByStatus,
    int TotalRequests,
    int PendingApprovalCount,
    int AwaitingAnalysisCount,
    int HighRiskPendingApprovalCount,
    int HighRiskCount,
    int CriticalRiskCount,
    int ReadyForDevelopmentCount,
    double? AverageApprovalTimeHours);

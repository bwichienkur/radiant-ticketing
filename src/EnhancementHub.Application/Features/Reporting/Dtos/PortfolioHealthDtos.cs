namespace EnhancementHub.Application.Features.Reporting.Dtos;

public sealed record PortfolioApplicationHealthDto(
    Guid ApplicationId,
    string ApplicationName,
    int UnresolvedDriftCount,
    int PendingRequestCount,
    int HighRiskPendingCount,
    int StaleRepositoryCount,
    int RiskScore);

public sealed record PortfolioHealthReportDto(
    IReadOnlyList<PortfolioApplicationHealthDto> Applications,
    DateTime GeneratedAtUtc);

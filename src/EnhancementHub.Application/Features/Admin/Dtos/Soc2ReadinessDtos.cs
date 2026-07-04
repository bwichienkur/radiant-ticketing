namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record Soc2ReadinessReportDto(
    int ImplementedCount,
    int PartialCount,
    int GapCount,
    IReadOnlyList<Soc2ControlStatusDto> Controls);

public sealed record Soc2ControlStatusDto(
    string ControlId,
    string TrustServiceCategory,
    string Title,
    string EnhancementHubFeature,
    string Status,
    string? ConfigurationHint);

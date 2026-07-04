namespace EnhancementHub.Application.Features.Reporting.Dtos;

public sealed record AiUsageReportDto(
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int TotalRuns,
    int TotalTokens,
    decimal TotalEstimatedCostUsd,
    IReadOnlyList<AiUsageByWorkflowDto> ByWorkflow,
    IReadOnlyList<AiUsageByModelDto> ByModel);

public sealed record AiUsageByWorkflowDto(
    string WorkflowStep,
    int RunCount,
    int TotalTokens,
    decimal EstimatedCostUsd);

public sealed record AiUsageByModelDto(
    string ModelName,
    int RunCount,
    int TotalTokens,
    decimal EstimatedCostUsd);

using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Analysis.Dtos;

public sealed record AnalysisComparisonDto(
    Guid EnhancementRequestId,
    int VersionA,
    int VersionB,
    RiskLevel RiskLevelA,
    RiskLevel RiskLevelB,
    double ConfidenceScoreA,
    double ConfidenceScoreB,
    IReadOnlyList<AnalysisFieldChangeDto> FieldChanges,
    IReadOnlyList<FindingComparisonDto> FindingChanges,
    int AiSuggestedFindingsA,
    int HumanApprovedFindingsB,
    int ArchitectEditsBetweenVersions);

public sealed record AnalysisFieldChangeDto(
    string FieldName,
    string? ValueA,
    string? ValueB,
    bool Changed);

public sealed record FindingComparisonDto(
    string Title,
    string Category,
    ComparisonChangeType ChangeType,
    bool IsAiSuggested,
    bool IsHumanApprovedInB);

public enum ComparisonChangeType
{
    Unchanged,
    Added,
    Removed,
    Modified
}
